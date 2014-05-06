using System;
using System.IO;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Partition
{
    public class StatelessFileQueueReaderTest
    {
        [Test]
        public void when_queue_not_init()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var result = new StatelessFileQueueReader(path, "test").TryGetMessage();

            Assert.AreEqual(GetEnvelopeResultState.Exception, result.State);
        }

        [Test]
        public void when_directory_empty()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, "test");
            queue.InitIfNeeded();
            var result = queue.TryGetMessage();

            Assert.AreEqual(GetEnvelopeResultState.Empty, result.State);
        }

        [Test]
        public void when_file_not_success()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, "test");
            queue.InitIfNeeded();
            using (var sw = new StreamWriter(Path.Combine(path, "0.dat"), false))
                sw.Write("test message");

            using (var stream = new FileStream(Path.Combine(path, "0.dat"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                var result = queue.TryGetMessage();

                Assert.AreEqual(GetEnvelopeResultState.Retry, result.State);
            }
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void when_ack_null_message()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, "test");
            queue.AckMessage(null);
        }

        [Test]
        public void when_ack_message()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, "test");
            queue.InitIfNeeded();
            using (var sw = new StreamWriter(Path.Combine(path, "0.dat"), false))
                sw.Write("test message");
            var result = queue.TryGetMessage();

            Assert.IsTrue(File.Exists(Path.Combine(path, "0.dat")));
            queue.AckMessage(result.Message);
            Assert.IsFalse(File.Exists(Path.Combine(path, "0.dat")));
        }
    }
}