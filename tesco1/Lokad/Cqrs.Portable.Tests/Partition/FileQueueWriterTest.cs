using System;
using System.IO;
using System.Text;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Partition
{
    public class FileQueueWriterTest
    {
        private string _path;
        [SetUp]
        public void Setup()
        {
            _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if(Directory.Exists(_path))
                Directory.Delete(_path,true);
        }

        [Test]
        public void when_put_message()
        {
            var bytes = Encoding.UTF8.GetBytes("test messages");
            
            var queueWriter = new FileQueueWriter(new DirectoryInfo(_path), "test");
            queueWriter.PutMessage(bytes);

            var files = new DirectoryInfo(_path).GetFiles();

            Assert.AreEqual(1, files.Length);
            Assert.AreEqual("test messages", File.ReadAllText(files[0].FullName));
        }
    }
}