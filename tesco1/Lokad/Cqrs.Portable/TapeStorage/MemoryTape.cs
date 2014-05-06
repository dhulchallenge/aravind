using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cqrs.TapeStorage
{
    public sealed class MemoryAppendOnlyStore : IAppendOnlyStore
    {
        readonly LockingInMemoryCache _cache = new LockingInMemoryCache();
 

        public void InitializeForWriting()
        {
            
        }

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {
            _cache.ConcurrentAppend(streamName, data, (version, storeVersion) => { }, expectedStreamVersion);
        }

        public IEnumerable<DataWithKey> ReadRecords(string streamName, long startingFrom, int maxCount)
        {
            return _cache.ReadStream(streamName, startingFrom, maxCount);
        }

        public IEnumerable<DataWithKey> ReadRecords(long startingFrom, int maxCount)
        {
            return _cache.ReadAll(startingFrom, maxCount);
        }

        public void Close()
        {
            
        }

        public void ResetStore()
        {
            _cache.Clear(() => { });
        }

        public long GetCurrentVersion()
        {
            return _cache.StoreVersion;
        }

        public void Dispose()
        {
            
        }
    }
}