using Lokad.Cqrs.Envelope;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Envelope
{
    public class DuplicationManagerTest
    {
        [Test]
        public void when_get()
        {
            var manager = new DuplicationManager();
            var memory1 = manager.GetOrAdd("dispatcher");
            var memory2 = manager.GetOrAdd("dispatcher");

            Assert.AreEqual(memory1, memory2);
        }
    }
}