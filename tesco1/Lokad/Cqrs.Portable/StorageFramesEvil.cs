using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Lokad.Cqrs
{
    /// <summary>
    /// Helps to persist sha1 hashed binary frames to stream
    /// and load them back. Good for key-value storage
    /// </summary>
    public static class StorageFramesEvil
    {
        sealed class BitReader : BinaryReader
        {
            public BitReader(Stream input) : base(input, Encoding.UTF8) { }

            public int Read7BitInt()
            {
                return Read7BitEncodedInt();
            }
            protected override void Dispose(bool disposing)
            {
                // we don't want to close underlying stream
                //base.Dispose(disposing);
            }
        }

        sealed class BitWriter : BinaryWriter
        {
            public BitWriter(Stream output) : base(output, Encoding.UTF8) { }
            public void Write7BitInt(int value)
            {
                Write7BitEncodedInt(value);
            }
            protected override void Dispose(bool disposing)
            {
                Flush();
            }
        }

        public static StorageFrameEncoded EncodeFrame(string key, byte[] buffer, long stamp)
        {
            using (var sha1 = new SHA1Managed())
            {
                // version, ksz, vsz, key, value, sha1
                byte[] data;
                using (var memory = new MemoryStream())
                {
                    using (var crypto = new CryptoStream(memory, sha1, CryptoStreamMode.Write))
                    using (var binary = new BitWriter(crypto))
                    {
                        binary.Write(stamp);
                        binary.Write(key);
                        binary.Write7BitInt(buffer.Length);
                        binary.Write(buffer);
                    }
                    data = memory.ToArray();

                }
                return new StorageFrameEncoded(data, sha1.Hash);
            }
        }

        public static void WriteFrame(string key, long stamp, byte[] buffer, Stream stream)
        {
            var frame = EncodeFrame(key, buffer, stamp);
            stream.Write(frame.Data, 0, frame.Data.Length);
            stream.Write(frame.Hash, 0, frame.Hash.Length);
        }

        public static StorageFrameDecoded ReadFrame(Stream source)
        {
            using (var binary = new BitReader(source))
            {
                var version = binary.ReadInt64();
                var name = binary.ReadString();
                var len = binary.Read7BitInt();
                var bytes = binary.ReadBytes(len);
                var sha1Expected = binary.ReadBytes(20);

                var decoded = new StorageFrameDecoded(bytes, name, version);
                if (decoded.IsEmpty && sha1Expected.All(b => b == 0))
                {
                    // this looks like end of the stream.
                    return decoded;
                }

                //SHA1. TODO: compute hash nicely
                var sha1Actual = EncodeFrame(name, bytes, version).Hash;
                if (!sha1Expected.SequenceEqual(sha1Actual))
                    throw new StorageFrameException("SHA mismatch in data frame");
                
                return decoded;
            }
        }

        public static bool TryReadFrame(Stream source, out StorageFrameDecoded result)
        {
            result = default(StorageFrameDecoded);
            try
            {
                result = ReadFrame(source);
                return !result.IsEmpty;
            }
            catch (EndOfStreamException)
            {
                // we are done
                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                // Auto-clean?
                return false;
            }
        }
    }

    /// <summary>
    /// Is thrown when there is a big problem with reading storage frame
    /// </summary>
    [Serializable]
    public class StorageFrameException : Exception
    {
        public StorageFrameException(string message) : base(message) { }
        protected StorageFrameException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }

    public struct StorageFrameEncoded
    {
        public readonly byte[] Data;
        public readonly byte[] Hash;

        public StorageFrameEncoded(byte[] data, byte[] hash)
        {
            Data = data;
            Hash = hash;
        }
    }

    public struct StorageFrameDecoded
    {
        public readonly byte[] Bytes;
        public readonly string Name;
        public readonly long Stamp;

        public bool IsEmpty
        {
            get { return Bytes.Length == 0 && Stamp == 0 && string.IsNullOrEmpty(Name); }
        }
            
        public StorageFrameDecoded(byte[] bytes, string name, long stamp)
        {
            Bytes = bytes;
            Name = name;
            Stamp = stamp;
        }
    }
}