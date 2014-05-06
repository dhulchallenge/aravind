using System;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public sealed class when_reading_stream_from_loaded_cache :fixture_with_cache_helpers
    {
        
        LockingInMemoryCache Cache;

        [SetUp]
        public void Setup()
        {
            Cache = new LockingInMemoryCache();

            Cache.LoadHistory(CreateFrames("stream1", "stream2"));
            Cache.ConcurrentAppend("stream1", GetEventBytes(3), (version, storeVersion) => { });

        }

        [Test]
        public void given_full_range()
        {
            DataAssert.AreEqual(new []
                {
                    CreateKey(1,1,"stream1"),
                    CreateKey(3,2, "stream1")
                }, Cache.ReadStream("stream1", 0, int.MaxValue));
        }

        [Test]
        public void given_tail_range()
        {
            DataAssert.AreEqual(new DataWithKey[]
                {
                    CreateKey(3,2,"stream1")
                }, Cache.ReadStream("stream1",1, 1));
        }

        [Test]
        public void given_nonmatching_range()
        {
            CollectionAssert.IsEmpty(Cache.ReadStream("stream1", 2, int.MaxValue));
        }

        [Test]
        public void given_non_matching_stream()
        {
            CollectionAssert.IsEmpty(Cache.ReadStream("streamZ", 0, int.MaxValue));
            
        }

        [Test]
        public void given_null_stream_name()
        {
            Assert.Throws<ArgumentNullException>(() => Cache.ReadStream(null, 0, int.MaxValue));
        }

        [Test]
        public void given_negative_stream_version()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cache.ReadStream("s", -1, int.MaxValue));
        }

        [Test]
        public void given_zero_count()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cache.ReadStream("s", 0, 0));
        }
    }
}