using System;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public sealed class when_reloading_all : fixture_with_cache_helpers
    {
        [Test]
        public void given_reloaded_cache()
        {
            var cache = new LockingInMemoryCache();
            cache.LoadHistory(CreateFrames("s1","s2"));

            Assert.Throws<InvalidOperationException>(() => cache.LoadHistory(CreateFrames("s1")));

        }

        [Test]
        public void given_empty_cache()
        {
            var cache = new LockingInMemoryCache();
            cache.Clear(() => { });
        }

        [Test]
        public void given_cleared_cache()
        {
            var cache = new LockingInMemoryCache();
            cache.LoadHistory(CreateFrames("s1", "s2"));
            cache.Clear(() => { });
            cache.LoadHistory(CreateFrames("s1"));

            Assert.AreEqual(1, cache.StoreVersion, "storeVersion");
        }

         
    }
}