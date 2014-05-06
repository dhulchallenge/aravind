using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Lokad.Cqrs.TapeStorage;
using Microsoft.WindowsAzure.StorageClient;
using Microsoft.WindowsAzure.StorageClient.Protocol;

namespace Lokad.Cqrs.AppendOnly
{
    /// <summary>
    /// <para>This is embedded append-only store implemented on top of cloud page blobs 
    /// (for persisting data with one HTTP call).</para>
    /// <para>This store ensures that only one writer exists and writes to a given event store</para>
    /// </summary>
    public sealed class BlobAppendOnlyStore : IAppendOnlyStore
    {
        // Caches
        readonly CloudBlobContainer _container;

        readonly LockingInMemoryCache _cache = new LockingInMemoryCache();


        bool _closed;

        /// <summary>
        /// Currently open file
        /// </summary>
        AppendOnlyStream _currentWriter;

        public BlobAppendOnlyStore(CloudBlobContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            if (!_closed)
                Close();
        }

        public void InitializeWriter()
        {
            CreateIfNotExists(_container, TimeSpan.FromSeconds(60));
            LoadCaches();
        }
        public void InitializeReader()
        {
            CreateIfNotExists(_container, TimeSpan.FromSeconds(60));
            LoadCaches();
        }


        int _pageSizeMultiplier = 1024 * 512;

        public void Append(string streamName, byte[] data, long expectedStreamVersion = -1)
        {
            // should be locked
            try
            {
                _cache.ConcurrentAppend(streamName, data, (streamVersion, storeVersion) =>
                {
                    EnsureWriterExists(storeVersion);
                    Persist(streamName, data, streamVersion);
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

        public IEnumerable<DataWithKey> ReadRecords(string streamName, long afterVersion, int maxCount)
        {
            return _cache.ReadStream(streamName, afterVersion, maxCount);
        }

        public IEnumerable<DataWithKey> ReadRecords(long afterVersion, int maxCount)
        {
            return _cache.ReadAll(afterVersion, maxCount);

        }

        public void Close()
        {
            _closed = true;

            if (_currentWriter == null)
                return;

            var tmp = _currentWriter;
            _currentWriter = null;
            tmp.Dispose();
        }

        public void ResetStore()
        {
            Close();
            _cache.Clear(() =>
            {
                var blobs = _container.ListBlobs().OfType<CloudPageBlob>().Where(item => item.Uri.ToString().EndsWith(".dat"));

                blobs
                    .AsParallel().ForAll(i => i.DeleteIfExists());
            });
        }

        public long GetCurrentVersion()
        {
            return _cache.StoreVersion;
        }



        IEnumerable<StorageFrameDecoded> EnumerateHistory()
        {
            // cleanup old pending files
            // load indexes
            // build and save missing indexes
            var datFiles = _container
                .ListBlobs(new BlobRequestOptions()
                {
                    BlobListingDetails = BlobListingDetails.Metadata
                })
                .OrderBy(s => s.Uri.ToString())
                .OfType<CloudPageBlob>()
                .Where(s => s.Name.EndsWith(".dat"));

            foreach (var fileInfo in datFiles)
            {

                var bytes = fileInfo.DownloadByteArray();
                bool potentiallyNonTruncatedChunk = bytes.Length % _pageSizeMultiplier == 0;
                long lastValidPosition = 0;
                using (var stream = new MemoryStream(bytes))
                {
                    StorageFrameDecoded result;
                    while (StorageFramesEvil.TryReadFrame(stream, out result))
                    {
                        lastValidPosition = stream.Position;
                        yield return result;
                    }
                }
                var haveSomethingToTruncate = bytes.Length - lastValidPosition >= 512;
                if (potentiallyNonTruncatedChunk & haveSomethingToTruncate)
                {
                    TruncateBlob(lastValidPosition, fileInfo);
                }
            }
        }

        void TruncateBlob(long lastValidPosition, CloudPageBlob fileInfo)
        {
            var trunc = lastValidPosition;
            var remainder = lastValidPosition % 512;
            if (remainder > 0)
            {
                trunc += 512 - remainder;
            }
            Trace.WriteLine(string.Format("Truncating {0} to {1}", fileInfo.Name, trunc));
            _container.GetPageBlobReference(fileInfo.Name + ".bak").CopyFromBlob(fileInfo);
            SetLength(fileInfo, trunc);
        }

        static void SetLength(CloudPageBlob blob, long newLength, int timeout = 10000)
        {
            var credentials = blob.ServiceClient.Credentials;

            var requestUri = blob.Uri;
            if (credentials.NeedsTransformUri)
                requestUri = new Uri(credentials.TransformUri(requestUri.ToString()));

            var request = BlobRequest.SetProperties(requestUri, timeout, blob.Properties, null, newLength);
            request.Timeout = timeout;

            credentials.SignRequest(request);

            using (request.GetResponse()) { }
        }



        void LoadCaches()
        {
            _cache.LoadHistory(EnumerateHistory());
        }

        void Persist(string key, byte[] buffer, long commit)
        {
            var frame = StorageFramesEvil.EncodeFrame(key, buffer, commit);
            if (!_currentWriter.Fits(frame.Data.Length + frame.Hash.Length))
            {
                CloseWriter();
                EnsureWriterExists(_cache.StoreVersion);
            }

            _currentWriter.Write(frame.Data);
            _currentWriter.Write(frame.Hash);
            _currentWriter.Flush();
        }

        void CloseWriter()
        {
            _currentWriter.Dispose();
            _currentWriter = null;
        }

        void EnsureWriterExists(long version)
        {
            if (_currentWriter != null)
                return;

            var fileName = string.Format("{0:00000000}-{1:yyyy-MM-dd-HHmmss}.dat", version, DateTime.UtcNow);
            var blob = _container.GetPageBlobReference(fileName);
            blob.Create(_pageSizeMultiplier);

            _currentWriter = new AppendOnlyStream(512, (i, bytes) => blob.WritePages(bytes, i), _pageSizeMultiplier);
        }

        static void CreateIfNotExists(CloudBlobContainer container, TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    container.CreateIfNotExist();
                    return;
                }
                catch (StorageClientException e)
                {
                    // container is being deleted
                    if (!(e.ErrorCode == StorageErrorCode.ResourceAlreadyExists && e.StatusCode == HttpStatusCode.Conflict))
                        throw;
                }
                Thread.Sleep(500);
            }

            throw new TimeoutException(string.Format("Can not create container within {0} seconds.", timeout.TotalSeconds));
        }
    }
}