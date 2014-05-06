using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Lokad.Cqrs.StreamingStorage
{
    [DataContract]
    public sealed class BinaryStreamReference
    {
        [DataMember(Order = 1)]
        public string Container { get; private set; }

        [DataMember(Order = 2)]
        public string Name { get; private set; }

        [DataMember(Order = 3)]
        public string Sha1 { get; private set; }

        [DataMember(Order = 4)]
        public int StorageSize { get; private set; }

        [DataMember(Order = 5)]
        public int ActualSize { get; private set; }

        [DataMember(Order = 6)]
        public bool Compressed { get; private set; }

        public BinaryStreamReference() { }
        public BinaryStreamReference(string container, string name, string sha1, int storageSize, int actualSize, bool compressed)
        {
            Container = container;
            Name = name;
            Sha1 = sha1;
            StorageSize = storageSize;
            ActualSize = actualSize;
            Compressed = compressed;
        }
    }

    public class BinaryStreamReader : IDisposable
    {
        readonly SHA1Managed _sha1 = new SHA1Managed();

        readonly Stream _stream;
        readonly CryptoStream _crypto;
        public readonly BinaryStreamReference Reference;
        readonly GZipStream _zip;
        bool _streamsDisposed;

        public Stream Stream
        {
            get { return _crypto; }
        }

        public BinaryStreamReader(Stream stream, BinaryStreamReference reference)
        {
            Reference = reference;
            _stream = stream;
            if (reference.Compressed)
            {
                _zip = new GZipStream(stream, CompressionMode.Decompress);
                _crypto = new CryptoStream(_zip, _sha1, CryptoStreamMode.Read);
            }
            else
            {
                _crypto = new CryptoStream(stream, _sha1, CryptoStreamMode.Read);
            }

        }

        byte[] _computedSha1;

        void DisposeStreamsIfNeeded()
        {
            if (_streamsDisposed) return;
            using (_sha1)
            {
                using (_stream)
                using (_zip)
                using (_crypto)
                {
                    _streamsDisposed = true;
                }
                _computedSha1 = _sha1.Hash;
            }
        }

        public void ReadRestOfStreamToComputeHashes()
        {
            var buffer = new byte[1024 * 10];
            while (true)
            {
                if (_crypto.Read(buffer, 0, buffer.Length) == 0)
                    return;
            }
        }

        public void CloseAndVerifyHash()
        {
            DisposeStreamsIfNeeded();
            var exectedSha1 = Reference.Sha1;
            if (null == exectedSha1)
                return;

            var computedSha1 = BitConverter.ToString(_computedSha1).Replace("-", "").ToLowerInvariant();

            if (!String.Equals(exectedSha1, computedSha1, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException(String.Format("Hash mismatch in {2}. Expected '{0}' was '{1}'",
                    exectedSha1, computedSha1, Reference));
        }


        public void Dispose()
        {
            DisposeStreamsIfNeeded();
        }
    }

    public class BinaryStreamWriter : IDisposable
    {
        readonly SHA1Managed _sha1 = new SHA1Managed();
        readonly string _fileName;
        readonly string _container;
        readonly Stream _stream;
        readonly CryptoStream _crypto;
        readonly GZipStream _zip;
        readonly StreamCounter _storageCounter;
        readonly StreamCounter _actualCounter;
        readonly bool _compress;
        public Stream Stream
        {
            get { return _actualCounter; }
        }

        public BinaryStreamWriter(Stream stream, string container, string fileName, bool compress)
        {
            _stream = stream;
            _container = container;
            _fileName = fileName;
            _storageCounter = new StreamCounter(_stream);
            if (compress)
            {
                _zip = new GZipStream(_storageCounter, CompressionMode.Compress, false);
                _crypto = new CryptoStream(_zip, _sha1, CryptoStreamMode.Write);
            }
            else
            {
                _crypto = new CryptoStream(_storageCounter, _sha1, CryptoStreamMode.Write);
            }
            _actualCounter = new StreamCounter(_crypto);
            _compress = compress;
        }

        bool _streamsDisposed;
        int _storageSize;
        int _actualSize;

        void DisposeStreamsIfNeeded()
        {
            if (_streamsDisposed) return;

            using (_sha1)
            {
                using (_storageCounter)
                using (_stream)
                using (_zip)
                using (_crypto)
                using (_actualCounter)
                {
                    _streamsDisposed = true;
                }
                _computedSha1 = BitConverter.ToString(_sha1.Hash).Replace("-", "").ToUpperInvariant();
                _storageSize = _storageCounter.WrittenBytes;
                _actualSize = _actualCounter.WrittenBytes;
            }
        }

        string _computedSha1;

        public BinaryStreamReference CloseAndComputeHash()
        {
            DisposeStreamsIfNeeded();
            return new BinaryStreamReference(_container, _fileName, _computedSha1, _storageSize, _actualSize, _compress);
        }

        public void Dispose()
        {
            DisposeStreamsIfNeeded();
        }
    }

    public sealed class StreamCounter : Stream
    {
        readonly Stream _stream;

        public int WrittenBytes { get; private set; }

        public StreamCounter(Stream stream)
        {
            _stream = stream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Seeking is not supported");
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Reading is not supported");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
            WrittenBytes += count;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { throw new NotSupportedException("Stream can't change position"); }
        }
    }
}