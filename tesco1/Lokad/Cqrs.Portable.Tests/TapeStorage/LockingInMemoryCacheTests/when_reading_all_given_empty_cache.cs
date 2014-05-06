using System;
using System.Linq;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{

    [TestFixture]
    public sealed class when_reading_all_given_empty_cache : fixture_with_cache_helpers
    {

        LockingInMemoryCache Cache;

        [SetUp]
        public void Setup()
        {
            Cache = new LockingInMemoryCache();
        }

        [Test]
        public void given_full_range()
        {
            CollectionAssert.IsEmpty(Cache.ReadAll(0, int.MaxValue));
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