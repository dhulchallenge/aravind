using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public sealed class when_clearing_cache : fixture_with_cache_helpers
    {

        [Test]
        public void given_empty_cache()
        {
            var cache = new LockingInMemoryCache();
            cache.Clear(() => { });
            Assert.AreEqual(0, cache.StoreVersion);
        }

        [Test]
        public void given_reloaded_cache()
        {
            var cache = new LockingInMemoryCache();
            cache.LoadHistory(CreateFrames("stream2"));
            cache.Clear(() => { });

            Assert.AreEqual(0, cache.StoreVersion);
        }


        [Test]
        public void given_appended_cache()
        {
            var cache = new LockingInMemoryCache();

            cache.ConcurrentAppend("stream1", new byte[1], (version, storeVersion) => { });

            cache.Clear(() => { });

            Assert.AreEqual(0, cache.StoreVersion);
        }

        [Test]
        public void given_filled_cache_and_failing_commit_function()
        {
            var cache = new LockingInMemoryCache();

            cache.ConcurrentAppend("stream1", new byte[1], (version, storeVersion) => { });

            Assert.Throws<FileNotFoundException>(() => cache.Clear(() =>
                {
                    throw new FileNotFoundException();
                }));

            Assert.AreEqual(1, cache.StoreVersion);
        }
    }
}