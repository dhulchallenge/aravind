#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Linq;
using System.Text;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.TapeStorage
{
    public class MemoryAppendOnlyStoreTest
    {
        private MemoryAppendOnlyStore _store;

        [SetUp]
        public void Setup()
        {
            _store = new MemoryAppendOnlyStore();
        }

        [TearDown]
        public void Teardown()
        {
            _store.ResetStore();
        }

        [Test]
        public void append_data()
        {
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

            _store.Append(key, Encoding.UTF8.GetBytes("test message1"), 100);
        }

        [Test]
        public void get_current_version()
        {
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message1"));
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message2"));
            _store.Append("versiontest", Encoding.UTF8.GetBytes("test message3"));

            Assert.AreEqual(3, _store.GetCurrentVersion());
        }

        [Test]
        public void read_all_records_by_stream()
        {
            var stream = Guid.NewGuid().ToString();

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

            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));
            _store.ResetStore();
            for (int i = 0; i < 10; i++)
                _store.Append(stream, Encoding.UTF8.GetBytes("test message" + i));

            var version = _store.GetCurrentVersion();

            Assert.GreaterOrEqual(10, version);
        } 
    }
}