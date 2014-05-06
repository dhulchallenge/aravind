using System;
using System.IO;
using Cqrs.Portable.Tests.Envelope;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class MessageSenderTest : specification_with_empty_directory
    {
       [Test]
        public void when_send_message()
        {
            var serializer = new TestMessageSerializer(new[] { typeof(SerializerTest1), typeof(SerializerTest2), });
            var streamer = new EnvelopeStreamer(serializer);
            var queueWriter = new FileQueueWriter(new DirectoryInfo(DirectoryPath), "test");
            var sender = new MessageSender(streamer, queueWriter);
            sender.Send(new SerializerTest1("Name1"), "EnvId", new[] { new MessageAttribute("key1", "val1"), new MessageAttribute("key2", "val2"), });
            sender.Send(new SerializerTest1("Name1"), "EnvId");

            Assert.AreEqual(2, Directory.GetFiles(DirectoryPath).Length);
        }
    }
}