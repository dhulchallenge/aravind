using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lokad.Cqrs.TapeStorage
{
    /// <summary>
    /// Simple in-memory thread-safe cache
    /// </summary>
    public sealed class LockingInMemoryCache
    {
        readonly ReaderWriterLockSlim _thread = new ReaderWriterLockSlim();
        ConcurrentDictionary<string, DataWithKey[]> _cacheByKey = new ConcurrentDictionary<string, DataWithKey[]>();
        DataWithKey[] _cacheFull = new DataWithKey[0];

        public void LoadHistory(IEnumerable<StorageFrameDecoded> sfd)
        {
            _thread.EnterWriteLock();
            try
            {
                if (StoreVersion != 0)
                    throw new InvalidOperationException("Must clear cache before loading history");

                _cacheFull = new DataWithKey[0];

                // [abdullin]: known performance problem identified by Nicolas Mehlei
                // creating new immutable array on each line will kill performance
                // We need to at least do some batching here

                var cacheFullBuilder = new List<DataWithKey>();
                var streamPointerBuilder = new Dictionary<string, List<DataWithKey>>();

                long newStoreVersion = 0;
                foreach (var record in sfd)
                {

                    List<DataWithKey> list;
                    if (!streamPointerBuilder.TryGetValue(record.Name, out list))
                    {
                        streamPointerBuilder.Add(record.Name, list = new List<DataWithKey>());
                    }

                    newStoreVersion += 1;
                    var newStreamVersion = list.Count + 1;

                    var data = new DataWithKey(record.Name, record.Bytes, newStreamVersion, newStoreVersion);
                    list.Add(data);
                    cacheFullBuilder.Add(data);
                }

                _cacheFull = cacheFullBuilder.ToArray();
                _cacheByKey = new ConcurrentDictionary<string, DataWithKey[]>(streamPointerBuilder.Select(p => new KeyValuePair<string, DataWithKey[]>(p.Key, p.Value.ToArray())));
                StoreVersion = newStoreVersion;
            }
            finally
            {
                _thread.ExitWriteLock();
            }
        }

        static T[] ImmutableAdd<T>(T[] source, T item)
        {
            var copy = new T[source.Length + 1];

            Array.Copy(source, copy, source.Length);
            copy[source.Length] = item;


            return copy;
        }

        public long StoreVersion { get; private set; }

        public delegate void OnCommit(long streamVersion, long storeVersion);

        public void ConcurrentAppend(string streamName, byte[] data, OnCommit commit, long expectedStreamVersion = -1)
        {
            _thread.EnterWriteLock();

            try
            {
                var list = _cacheByKey.GetOrAdd(streamName, s => new DataWithKey[0]);
                var actualStreamVersion = list.Length;

                if (expectedStreamVersion >= 0)
                {
                    if (actualStreamVersion != expectedStreamVersion)
                        throw new AppendOnlyStoreConcurrencyException(expectedStreamVersion, actualStreamVersion, streamName);
                }
                long newStreamVersion = actualStreamVersion + 1;
                long newStoreVersion = StoreVersion + 1;

                commit(newStreamVersion, newStoreVersion);

                // update in-memory cache only after real commit completed

                
                var dataWithKey = new DataWithKey(streamName, data, newStreamVersion, newStoreVersion);
                _cacheFull = ImmutableAdd(_cacheFull, dataWithKey);
                _cacheByKey.AddOrUpdate(streamName, s => new[] { dataWithKey }, (s, records) => ImmutableAdd(records, dataWithKey));
                StoreVersion = newStoreVersion;

            }
            finally
            {
                _thread.ExitWriteLock();
            }

        }

        public IEnumerable<DataWithKey> ReadStream(string streamName, long afterStreamVersion, int maxCount)
        {
            if (null == streamName)
                throw new ArgumentNullException("streamName");
            if (afterStreamVersion < 0)
                throw new ArgumentOutOfRangeException("afterStreamVersion", "Must be zero or greater.");

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount", "Must be more than zero.");

            // no lock is needed.
            DataWithKey[] list;
            var result = _cacheByKey.TryGetValue(streamName, out list) ? list : Enumerable.Empty<DataWithKey>();

            return result.Where(version => version.StreamVersion > afterStreamVersion).Take(maxCount);

        }

        public IEnumerable<DataWithKey> ReadAll(long afterStoreVersion, int maxCount)
        {
            if (afterStoreVersion < 0)
                throw new ArgumentOutOfRangeException("afterStoreVersion", "Must be zero or greater.");

            if (maxCount <= 0)
                throw new ArgumentOutOfRangeException("maxCount", "Must be more than zero.");


            // collection is immutable so we don't care about locks
            return _cacheFull.Where(key => key.StoreVersion > afterStoreVersion).Take(maxCount);
        }

        public void Clear(Action executeWhenCommitting)
        {
            _thread.EnterWriteLock();
            try
            {
                executeWhenCommitting();
                _cacheFull = new DataWithKey[0];
                _cacheByKey.Clear();
                StoreVersion = 0;
            }
            finally
            {
                _thread.ExitWriteLock();
            }
        }
    }
}