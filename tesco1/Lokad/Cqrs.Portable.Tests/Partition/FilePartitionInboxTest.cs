using System;
using System.IO;
using System.Threading;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Partition
{
    public class FilePartitionInboxTest
    {
        [Test]
        public void when_init_of_needed()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, "test");

            var inbox = new FileQueueReader(new[] { queue }, x => new TimeSpan(x));

            Assert.IsFalse(Directory.Exists(path));
            inbox.InitIfNeeded();
            Assert.IsTrue(Directory.Exists(path));
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void when_ack_null_message()
        {
            var queue1 = InitQueue("test1");

            var inbox = new FileQueueReader(new[] { queue1 }, x => new TimeSpan(x));
            inbox.InitIfNeeded();
            inbox.AckMessage(null);
        }

        [Test]
        public void when_ack_messages_by_name()
        {
            var queue1 = InitQueue("test1");

            var inbox = new FileQueueReader(new[] { queue1 }, x => new TimeSpan(x));
            inbox.InitIfNeeded();
            MessageTransportContext message1;
            inbox.TakeMessage(new CancellationToken(false), out message1);
            inbox.AckMessage(message1);

            Assert.IsFalse(new FileInfo(((FileInfo)message1.TransportMessage).FullName).Exists);
        }

        private static StatelessFileQueueReader InitQueue(string name)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var queue = new StatelessFileQueueReader(path, name);
            queue.InitIfNeeded();
            using (var sw = new StreamWriter(Path.Combine(path, "0.dat"), false))
                sw.Write("test queue");
            return queue;
        }

        [Test]
        public void when_take_message()
        {
            var queue1 = InitQueue("test1");

            var inbox = new FileQueueReader(new[] { queue1 }, x => new TimeSpan(x));
            inbox.InitIfNeeded();
            MessageTransportContext message1;
            inbox.TakeMessage(new CancellationToken(false), out message1);

            Assert.AreEqual("test1", message1.QueueName); 
        }
    }
}