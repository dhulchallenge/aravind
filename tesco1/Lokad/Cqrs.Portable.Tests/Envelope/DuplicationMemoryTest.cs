using System;
using Lokad.Cqrs.Envelope;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Envelope
{
    public class DuplicationMemoryTest
    {
        [Test]
        public void when_exist_key()
        {
            var memory = new DuplicationMemory();
            memory.Memorize("MemId");
            var result = memory.DoWeRemember("MemId");

            Assert.IsTrue(result);
        }

        [Test]
        public void when_not_contains_key()
        {
            var memory = new DuplicationMemory();
            var result = memory.DoWeRemember("MemId");

            Assert.IsFalse(result);
        }

        [Test]
        public void when_forget_older()
        {
            var memory = new DuplicationMemory();
            memory.Memorize("MemId");
            memory.ForgetOlderThan(new TimeSpan(0));
            var result = memory.DoWeRemember("MemId");

            Assert.IsFalse(result);
        }
    }
}