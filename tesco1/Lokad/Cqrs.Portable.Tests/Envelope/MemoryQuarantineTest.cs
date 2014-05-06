using System;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Envelope
{
    public class MemoryQuarantineTest
    {
        [Test]
        public void when_try_to_quarantine_and_envelope_null()
        {
            var result = new MemoryQuarantine().TryToQuarantine(null, new Exception());

            Assert.IsTrue(result);
        }

        [Test]
        public void when_more_try_to_quarantine()
        {
            var memoryQuarantine = new MemoryQuarantine();
            var immutableEnvelope = new ImmutableEnvelope("EnvId", DateTime.UtcNow, new SerializerTest1 { Name = "Test1" },
                                           new[]
                                               {
                                                   new MessageAttribute("key1", "val1"),
                                                   new MessageAttribute("key2", "val2"),
                                               });

            const int callCount = 30;
            var results = new bool[callCount];

            for (int i = 0; i < callCount; i++)
            {
                results[i] = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            }

            for (int i = 0; i < callCount; i++)
            {
                if ((i + 1) % 4 == 0)
                    Assert.IsTrue(results[i]);
                else
                    Assert.IsFalse(results[i]);
            }
        }

        [Test]
        public void when_try_release()
        {
            var memoryQuarantine = new MemoryQuarantine();
            var immutableEnvelope = new ImmutableEnvelope("EnvId", DateTime.UtcNow, new SerializerTest1 { Name = "Test1" },
                                           new[]
                                               {
                                                   new MessageAttribute("key1", "val1"),
                                                   new MessageAttribute("key2", "val2"),
                                               });
            var result0 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            memoryQuarantine.TryRelease(immutableEnvelope);
            var result1 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result2 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result3 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result4 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());

            Assert.IsFalse(result0);
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);
            Assert.IsTrue(result4);
        }

        [Test]
        public void when_try_release_where_envelope_null()
        {
            var memoryQuarantine = new MemoryQuarantine();
            var immutableEnvelope = new ImmutableEnvelope("EnvId", DateTime.UtcNow, new SerializerTest1 { Name = "Test1" },
                                           new[]
                                               {
                                                   new MessageAttribute("key1", "val1"),
                                                   new MessageAttribute("key2", "val2"),
                                               });
            var result0 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            memoryQuarantine.TryRelease(null);
            var result1 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result2 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result3 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());
            var result4 = memoryQuarantine.TryToQuarantine(immutableEnvelope, new Exception());

            Assert.IsFalse(result0);
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsTrue(result3);
            Assert.IsFalse(result4);
        }
    }
}