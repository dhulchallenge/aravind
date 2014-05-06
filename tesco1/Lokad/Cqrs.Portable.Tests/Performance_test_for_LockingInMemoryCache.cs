using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lokad.Cqrs;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

// ReSharper disable InconsistentNaming 
namespace Cqrs.Portable.Tests
{
    [TestFixture]

    public sealed class Performance_test_for_LockingInMemoryCache
    {
        [Test, Explicit]
        public void Name()
        {
            var cache = new LockingInMemoryCache();

            var watch = Stopwatch.StartNew();
            var count = 100000;
            cache.LoadHistory(Generate(count, new byte[200], i => string.Format("stream_{0}", i % 100)));
            watch.Stop();
            Console.WriteLine("Cached {0} events in {1:0.00} sec. {2:0.00} eps", count, (watch.Elapsed.TotalSeconds),
                count / watch.Elapsed.TotalSeconds);

        } 

        IEnumerable<StorageFrameDecoded> Generate(int count, byte[] buffer,Func<int,string> streamName)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new StorageFrameDecoded(buffer, streamName(i), i);
            }
        } 
    }
}