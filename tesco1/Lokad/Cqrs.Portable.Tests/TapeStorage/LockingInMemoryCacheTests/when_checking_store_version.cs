using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

// ReSharper disable InconsistentNaming 
namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public class when_checking_store_version
    {

        [Test]
        public void given_empty_cache()
        {
            Assert.AreEqual(0,new LockingInMemoryCache().StoreVersion);
        }

        [Test]
        public void given_cache_with_one_appended_record()
        {
            var cache = new LockingInMemoryCache();
            cache.ConcurrentAppend("Stream", new byte[0], (version, storeVersion) => { }, -1);

            Assert.AreEqual(1, cache.StoreVersion);
        }

        [Test]
        public void given_empty_reload()
        {
            var cache = new LockingInMemoryCache();
            cache.LoadHistory(Enumerable.Empty<StorageFrameDecoded>());
            Assert.AreEqual(0, cache.StoreVersion);
        }

        [Test]
        public void given_non_empty_reload()
        {
            var cache = new LockingInMemoryCache();
            cache.LoadHistory(new StorageFrameDecoded[]
                {
                    new StorageFrameDecoded(new byte[1], "test",0), 
                });

            Assert.AreEqual(1, cache.StoreVersion);
        }
    }

    
}