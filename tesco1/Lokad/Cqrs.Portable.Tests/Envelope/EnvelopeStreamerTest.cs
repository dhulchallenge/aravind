using System;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Envelope
{
    public class EnvelopeStreamerTest
    {
        [Test]
        public void when_write_and_read_envelope()
        {
            var serializer = new TestMessageSerializer(new[] { typeof(SerializerTest1), typeof(SerializerTest2), });
            var streamer = new EnvelopeStreamer(serializer);

            var date = DateTime.UtcNow;
            var savedBytes = streamer.SaveEnvelopeData(new ImmutableEnvelope("EnvId", date, new SerializerTest1 { Name = "Test1" },
                                                             new[]
                                                                {
                                                                    new MessageAttribute("key1", "val1"),
                                                                    new MessageAttribute("key2", "val2"),
                                                                }));

            var envelope = streamer.ReadAsEnvelopeData(savedBytes);

            Assert.AreEqual("EnvId", envelope.EnvelopeId);
            Assert.AreEqual(date, envelope.CreatedUtc);
            Assert.AreEqual(2, envelope.Attributes.Count);
            Assert.AreEqual("key1", envelope.Attributes.ToArray()[0].Key);
            Assert.AreEqual("val1", envelope.Attributes.ToArray()[0].Value);
            Assert.AreEqual("key2", envelope.Attributes.ToArray()[1].Key);
            Assert.AreEqual("val2", envelope.Attributes.ToArray()[1].Value);
            Assert.AreEqual(typeof(SerializerTest1), envelope.Message.GetType());
            Assert.AreEqual("Test1", (envelope.Message as SerializerTest1).Name);
        }
    }
}