using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lokad.Cqrs.TapeStorage
{
    /// <summary>
    /// Simple embedded append-only store that uses Riak.Bitcask model
    /// for keeping records
    /// </summary>
    public class FileAppendOnlyStore : IAppendOnlyStore
    {
        readonly DirectoryInfo _info;

        // used to synchronize access between threads within a process


        // used to prevent writer access to store to other processes
        FileStream _lock;
        FileStream _currentWriter;

        readonly LockingInMemoryCache _cache = new LockingInMemoryCache();
        // caches


        public void Initialize()
        {
            _info.Refresh();
            if (!_info.Exists)
                _info.Create();
            // grab the ownership
            _lock = new FileStream(Path.Combine(_info.FullName, "lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                8,
                FileOptions.DeleteOnClose);

            LoadCaches();
        }



        public void LoadCaches()
        {
            _cache.LoadHistory(EnumerateHistory());
        }

        IEnumerable<StorageFrameDecoded> EnumerateHistory()
        {
            // cleanup old pending files
            // load indexes
            // build and save missing indexes
            var datFiles = _info.EnumerateFiles("*.dat");

            foreach (var fileInfo in datFiles.OrderBy(fi => fi.Name))
            {
                // quick cleanup
                if (fileInfo.Length == 0)
                {
                    fileInfo.Delete();
                    continue;
                }

                using (var reader = fileInfo.OpenRead())
                {
                    StorageFrameDecoded result;
                    while (StorageFramesEvil.TryReadFrame(reader, out result))
                    {
                        yield return result;
                    }
                }
            }
        }


        public void Dispose()
        {
            if (!_closed)
                Close();
        }

        public FileAppendOnlyStore(DirectoryInfo info)
        {
            _info = info;
        }

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {
            // should be locked
            try
            {

                _cache.ConcurrentAppend(streamName, data, (streamVersion, storeVersion) =>
                {
                    EnsureWriterExists(storeVersion);
                    PersistInFile(streamName, data, streamVersion);
                }, expectedStreamVersion);

            }
            catch (AppendOnlyStoreConcurrencyException)
            {
                //store is OK when AOSCE is thrown. This is client's problem
                // just bubble it upwards
                throw;
            }
            catch
            {
                // store probably corrupted. Close it and then rethrow exception
                // so that clien will have a chance to retry.
                Close();
                throw;
            }

        }

        void PersistInFile(string key, byte[] buffer, long streamVersion)
        {
            StorageFramesEvil.WriteFrame(key, streamVersion, buffer, _currentWriter);
            // make sure that we persist
            // NB: this is not guaranteed to work on Linux
            _currentWriter.Flush(true);
        }

        void EnsureWriterExists(long storeVersion)
        {
            if (_currentWriter != null) return;

            var fileName = string.Format("{0:00000000}-{1:yyyy-MM-dd-HHmmss}.dat", storeVersion, DateTime.UtcNow);
            _currentWriter = File.OpenWrite(Path.Combine(_info.FullName, fileName));
        }

        public IEnumerable<DataWithKey> ReadRecords(string streamName, long afterVersion, int maxCount)
        {
            return _cache.ReadStream(streamName, afterVersion, maxCount);
        }

        public IEnumerable<DataWithKey> ReadRecords(long afterVersion, int maxCount)
        {
            return _cache.ReadAll(afterVersion, maxCount);

        }

        bool _closed;

        public void Close()
        {
            using (_lock)
            using (_currentWriter)
            {
                _currentWriter = null;
                _closed = true;

            }
        }

        public void ResetStore()
        {
            Close();
            _cache.Clear(() => Directory.Delete(_info.FullName, true));
            Initialize();
        }


        public long GetCurrentVersion()
        {
            return _cache.StoreVersion;
        }
    }
}