using System;
using System.IO;
using Lokad.Cqrs.AppendOnly;
using NUnit.Framework;

namespace Cqrs.Azure.Tests.AppendOnly
{
    public class AppendOnlyStreamTest
    {
        private AppendOnlyStream _appendOnlyStore;

        private Stream _stream;

        [SetUp]
        public void Setup()
        {
            _stream = null;
            const int pageSizeInBytes = 5;
            _appendOnlyStore = new AppendOnlyStream(pageSizeInBytes,
                (o, s) =>
                {
                    if (s.Length > 0)
                    {
                        if (_stream == null)
                            _stream = new MemoryStream();
                        s.Position = 0;
                        var bytes = new byte[s.Length];
                        s.Read(bytes, 0, (int)s.Length);
                        _stream.Position = o;
                        _stream.Write(bytes, 0, bytes.Length);
                    }
                }
            , 100);
        }

        [TearDown]
        public void TearDown()
        {
            _appendOnlyStore.Dispose();
        }

        [Test]
        public void when_bytes_more_than_maximum()
        {
            Console.WriteLine("starting when_bytes_more_than_maximum");
            var result = _appendOnlyStore.Fits(101);

            Assert.IsFalse(result);
        }

        [Test]
        public void when_bytes_less_than_maximum()
        {
            var result = _appendOnlyStore.Fits(99);

            Assert.IsTrue(result);
        }

        [Test]
        public void when_flush_empty_stream()
        {
            _appendOnlyStore.Flush();

            Assert.AreEqual(0, _appendOnlyStore.PersistedPosition);
            Assert.IsNull(_stream);
        }

        [Test]
        public void when_flush_bytes_less_than_page_size()
        {
            _appendOnlyStore.Write(new byte[] { 1, 2, 3, 4 });
            _appendOnlyStore.Flush();

            Assert.AreEqual(4, _appendOnlyStore.PersistedPosition);
            Assert.AreEqual(5, _stream.Length);
        }

        [Test]
        public void when_flush_bytes_more_than_page_size()
        {
            _appendOnlyStore.Write(new byte[] { 1, 2, 3, 4, 5, 6 });
            _appendOnlyStore.Flush();

            Assert.AreEqual(6, _appendOnlyStore.PersistedPosition);
            Assert.AreEqual(10, _stream.Length);
            _stream.Position = 0;
            for (int i = 0; i < 10; i++)
            {
                if (i < 6)
                    Assert.AreEqual(i + 1, _stream.ReadByte());
                else
                    Assert.AreEqual(0, _stream.ReadByte());
            }
        }

        [Test]
        public void when_more_call_flush_bytes_more_than_page_size()
        {
            _appendOnlyStore.Write(new byte[] { 1, 2, 3 });

            Assert.AreEqual(0, _appendOnlyStore.PersistedPosition);
            _appendOnlyStore.Flush();
            Assert.AreEqual(3, _appendOnlyStore.PersistedPosition);


            _appendOnlyStore.Write(new byte[] { 4, 5 });
            Assert.AreEqual(3, _appendOnlyStore.PersistedPosition);
            _appendOnlyStore.Flush();
            Assert.AreEqual(5, _appendOnlyStore.PersistedPosition);
            _appendOnlyStore.Write(new byte[] { 6 });
            Assert.AreEqual(5, _appendOnlyStore.PersistedPosition);
            _appendOnlyStore.Flush();
            Assert.AreEqual(6, _appendOnlyStore.PersistedPosition);
            Assert.AreEqual(10, _stream.Length);
            _stream.Position = 0;
            for (int i = 0; i < 10; i++)
            {
                if (i < 6)
                    Assert.AreEqual(i + 1, _stream.ReadByte());
                else
                    Assert.AreEqual(0, _stream.ReadByte());
            }
        }
    }
}