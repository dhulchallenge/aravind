using System;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public sealed class when_reading_all_given_filled_cache : fixture_with_cache_helpers
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
        public void given_non_matching_range()
        {
            
            CollectionAssert.IsEmpty(Cache.ReadAll(3, 10));
        }

        [Test]
        public void given_intersecting_range()
        {
            
            var dataWithKeys = Cache.ReadAll(1, 1);
            DataAssert.AreEqual(new[] { CreateKey(2, 1, "stream2") }, dataWithKeys);
        }


        [Test]
        public void given_matching_range()
        {
            var dataWithKeys = Cache.ReadAll(0, 3);
            DataAssert.AreEqual(new[]
                {
                    CreateKey(1, 1, "stream1"),
                    CreateKey(2, 1, "stream2"),
                    CreateKey(3, 2, "stream1")
                }, dataWithKeys);
        }

        [Test]
        public void given_full_range()
        {
            var dataWithKeys = Cache.ReadAll(0, int.MaxValue);
            DataAssert.AreEqual(new[]
                {
                    CreateKey(1, 1, "stream1"),
                    CreateKey(2, 1, "stream2"),
                    CreateKey(3, 2, "stream1")
                }, dataWithKeys);
        }

        [Test]
        public void given_negative_store_version()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cache.ReadAll(-1, int.MaxValue));
        }

        [Test]
        public void given_zero_count()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Cache.ReadAll(0, 0));
        }




    }
}