using System;
using System.IO;
using System.Linq;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage
{
    public class FileAppendOnlyStoreTest
    {
        private readonly string _storePath = Path.Combine(Path.GetTempPath(), "Lokad-CQRS");
        private FileAppendOnlyStore _store;
        private const int DataFileCount = 10;
        private const int FileMessagesCount = 5;

        [SetUp]
        public void Setup()
        {
            _store = new FileAppendOnlyStore(new DirectoryInfo(_storePath));
        }

        [TearDown]
        public void Teardown()
        {
            _store.ResetStore();
            _store.Close();
        }

        void CreateCacheFiles()
        {
            const string msg = "test messages";
            Directory.CreateDirectory(_storePath);
            for (int index = 0; index < DataFileCount; index++)
            {
                using (var stream = new FileStream(Path.Combine(_storePath, index + ".dat"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    for (int i = 0; i < FileMessagesCount; i++)
                    {
                        StorageFramesEvil.WriteFrame("test-key" + index, i, Encoding.UTF8.GetBytes(msg + i), stream);
                    }
                }
            }
        }

        [Test]
        public void load_cache()
        {
            CreateCacheFiles();
            _store.LoadCaches();

            for (int j = 0; j < DataFileCount; j++)
            {
                var key = "test-key" + j;
                var data = _store.ReadRecords(key, 0, Int32.MaxValue);

                int i = 0;
                foreach (var dataWithKey in data)
                {
                    Assert.AreEqual("test messages" + i, Encoding.UTF8.GetString(dataWithKey.Data));
                    i++;
                }
                Assert.AreEqual(FileMessagesCount, i);
            }
        }

        [Test]
        public void load_cache_when_exist_empty_file()
        {
            //write frame
            using (var stream = new FileStream(Path.Combine(_storePath, "0.dat"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                StorageFramesEvil.WriteFrame("test-key", 0, Encoding.UTF8.GetBytes("test message"), stream);

            //create empty file
            using (var sw = new StreamWriter(Path.Combine(_storePath, "1.dat")))
                sw.Write("");

            _store.LoadCaches();
            var data = _store.ReadRecords(0, Int32.MaxValue).ToArray();


            Assert.AreEqual(1, data.Length);
            Assert.AreEqual("test-key", data[0].Key);
            Assert.AreEqual(1, data[0].StreamVersion);
            Assert.AreEqual("test message", Encoding.UTF8.GetString(data[0].Data));
            Assert.IsFalse(File.Exists(Path.Combine(_storePath, "1.dat")));
        }

        [Test]
        public void load_cache_when_incorrect_data_file()
        {
            //write frame
            var path = Path.Combine(_storePath, "0.dat");
            using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                StorageFramesEvil.WriteFrame("test-key", 0, Encoding.UTF8.GetBytes("test message"), stream);

            //write incorrect frame
            using (var sw = new StreamWriter(Path.Combine(_storePath, "1.dat")))
                sw.Write("incorrect frame data");

            _store.LoadCaches();
            var data = _store.ReadRecords(0, Int32.MaxValue).ToArray();


            Assert.AreEqual(1, data.Length);
            Assert.AreEqual("test-key", data[0].Key);
            Assert.AreEqual(1, data[0].StreamVersion);
            Assert.AreEqual("test message", Encoding.UTF8.GetString(data[0].Data));
            Assert.IsTrue(File.Exists(Path.Combine(_storePath, "1.dat")));
        }

        [Test]
        public void append_data()
        {
            _store.Initialize();
            var currentVersion = _store.GetCurrentVersion();
            const int messagesCount = 3;
            for (int i = 0; i < messagesCount; i++)
            {
                _store.Append("stream1", Encoding.UTF8.GetBytes("test message" + i));
            }

            var data = _store.ReadRecords("stream1", currentVersion, Int32.MaxValue).ToArray();

            for (int i = 0; i < messagesCount; i++)
            {
                Assert.AreEqual("test message" + i, Encoding.UTF8.GetString(data[i].Data));
            }

            Assert.AreEqual(messagesCount, data.Length);
        }

        [Test, ExpectedException(typeof(AppendOnlyStoreConcurrencyException))]
        public void append_data_when_set_version_where_does_not_correspond_real_version()
        {
            var key = Guid.NewGuid().ToString();

            _store.Initialize();
            _store.Append(key, Encoding.UTF8.GetBytes("test message1"), 100);
        }

        [Test]
        public void get_current_version()
        {
            _store.Initialize();
            var currentVersion = _store.GetCurrentVersion();
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message1"));
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message2"));
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message3"));

            Assert.AreEqual(currentVersion + 3, _store.GetCurrentVersion());
        }

        [Test]
        public void read_all_records_by_stream()
        {
            var stream = Guid.NewGuid().ToString();

            _store.Initialize();
            for (int i = 0; i < 2; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var records = _store.ReadRecords(stream, 0, Int32.MaxValue).ToArray();

            Assert.AreEqual(2, records.Length);

            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual("test message" + i, Encoding.UTF8.GetString(records[i].Data));
                Assert.AreEqual(i + 1, records[i].StreamVersion);
            }
        }

        [Test]
        public void read_records_by_stream_after_version()
        {
            var stream = Guid.NewGuid().ToString();

            _store.Initialize();
            var currentVersion = _store.GetCurrentVersion();

            for (int i = 0; i < 2; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var records = _store.ReadRecords(stream, currentVersion + 1, Int32.MaxValue).ToArray();

            Assert.AreEqual(1, records.Length);
            Assert.AreEqual("test message1", Encoding.UTF8.GetString(records[0].Data));
            Assert.AreEqual(2, records[0].StreamVersion);
        }

        [Test]
        public void read_store_all_records()
        {
            var stream = Guid.NewGuid().ToString();
            _store.Initialize();
            var currentVersion = _store.GetCurrentVersion();
            for (int i = 0; i < 2; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var records = _store.ReadRecords(0, Int32.MaxValue).ToArray();

            Assert.AreEqual(currentVersion + 2, records.Length);

            for (var i = currentVersion; i < currentVersion + 2; i++)
            {
                Assert.AreEqual("test message" + (i - currentVersion), Encoding.UTF8.GetString(records[i].Data));
                Assert.AreEqual(i - currentVersion + 1, records[i].StreamVersion);
                Assert.AreEqual(i + 1, records[i].StoreVersion);
            }
        }

        [Test]
        public void read_store_records_after_version()
        {
            var stream = Guid.NewGuid().ToString();
            _store.Initialize();
            var currentVersion = _store.GetCurrentVersion();
            for (int i = 0; i < 2; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var records = _store.ReadRecords(currentVersion + 1, Int32.MaxValue).ToArray();

            Assert.AreEqual(1, records.Length);
            Assert.AreEqual("test message1", Encoding.UTF8.GetString(records[0].Data));
            Assert.AreEqual(2, records[0].StreamVersion);
            Assert.AreEqual(currentVersion + 2, records[0].StoreVersion);

        }

        [Test]
        public void when_reset_store()
        {
            var stream = Guid.NewGuid().ToString();

            _store.Initialize();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var version = _store.GetCurrentVersion();
            _store.ResetStore();
            var versionAfterReset = _store.GetCurrentVersion();

            Assert.GreaterOrEqual(10, version);
            Assert.AreEqual(0, versionAfterReset);
        }

        [Test]
        public void when_append_after_reset_store()
        {
            var stream = Guid.NewGuid().ToString();

            _store.Initialize();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));
            _store.ResetStore();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var version = _store.GetCurrentVersion();

            Assert.GreaterOrEqual(10, version);
        }

        [Test]
        public void when_more_call_reset()
        {
            var stream = Guid.NewGuid().ToString();

            _store.Initialize();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));
            _store.ResetStore();
            _store.ResetStore();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var version = _store.GetCurrentVersion();

            Assert.GreaterOrEqual(10, version);
        }
    }
}