using System;
using System.Collections.Generic;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage.LockingInMemoryCacheTests
{
    [TestFixture]
    public sealed class when_reading_stream_from_empty_cache : fixture_with_cache_helpers
    {

        LockingInMemoryCache Cache;

        [SetUp]
        public void Setup()
        {
            Cache = new LockingInMemoryCache();
        }

        [Test]
        public void given_any_stream()
        {
            CollectionAssert.IsEmpty(Cache.ReadStream("stream1", 0, int.MaxValue));
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