using System;
using System.IO;
using System.Linq;
using Cqrs.Portable.Tests.Envelope;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class MessageStoreTest
    {
        private string _path;
        private FileAppendOnlyStore _appendOnlyStore;
        IMessageSerializer _serializer;
        private MessageStore _store;

        [SetUp]
        public void SetUp()
        {
            _serializer = new TestMessageSerializer(new[] { typeof(SerializerTest1), typeof(SerializerTest2), typeof(string) });
            _path = Path.Combine(Path.GetTempPath(), "MessageStore", Guid.NewGuid().ToString());
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);
            _appendOnlyStore = new FileAppendOnlyStore(new DirectoryInfo(_path));

            var store = new MessageStore(_appendOnlyStore, _serializer);
            store.AppendToStore("stream1", MessageAttribute.Empty, -1, new[] { new SerializerTest1("msg1") });
            store.AppendToStore("stream2", MessageAttribute.Empty, -1, new[] { new SerializerTest1("msg1"), new SerializerTest1("msg2") });
            store.AppendToStore("stream3", MessageAttribute.Empty, -1, new[] { new SerializerTest1("msg1"), new SerializerTest1("msg2"), new SerializerTest1("msg3") });
            store.RecordMessage("stream4", new ImmutableEnvelope("EnvId", DateTime.UtcNow, new SerializerTest1("msg1"), MessageAttribute.Empty));

            _appendOnlyStore.Close();
            _appendOnlyStore = new FileAppendOnlyStore(new DirectoryInfo(_path));
            _appendOnlyStore.Initialize();

            _store = new MessageStore(_appendOnlyStore, _serializer);
        }

        [TearDown]
        public void TearDown()
        {
            _appendOnlyStore.Close();
            if (Directory.Exists(_path))
                Directory.Delete(_path, true);
        }

        [Test]
        public void when_enumerate_messages()
        {
            var records = _store.EnumerateMessages("stream3", 0, Int32.MaxValue).ToArray();

            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(3, records[0].Items.Length);
            Assert.AreEqual("msg1", (records[0].Items[0] as SerializerTest1).Name);
            Assert.AreEqual("msg2", (records[0].Items[1] as SerializerTest1).Name);
            Assert.AreEqual("msg3", (records[0].Items[2] as SerializerTest1).Name);
        }

        [Test]
        public void when_enumerate_messages_with_max_count()
        {
            var records = _store.EnumerateMessages("stream3", 0, 2).ToArray();

            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(3, records[0].Items.Length);
            Assert.AreEqual("msg1", (records[0].Items[0] as SerializerTest1).Name);
            Assert.AreEqual("msg2", (records[0].Items[1] as SerializerTest1).Name);
            Assert.AreEqual("msg3", (records[0].Items[2] as SerializerTest1).Name);
        }

        [Test]
        public void when_get_version()
        {
            var version = _store.GetVersion();

            Assert.AreEqual(4, version);
        }

        [Test]
        public void when_get_all_items()
        {
            var records = _store.EnumerateAllItems(0, 2).ToArray();

            Assert.AreEqual(2, records.Length);
            Assert.AreEqual(1, records[0].Items.Length);
            Assert.AreEqual("msg1", (records[0].Items[0] as SerializerTest1).Name);
            Assert.AreEqual(2, records[1].Items.Length);
            Assert.AreEqual("msg1", (records[1].Items[0] as SerializerTest1).Name);
            Assert.AreEqual("msg2", (records[1].Items[1] as SerializerTest1).Name);
        }

        [Test]
        public void when_get_records_whivh_add_record_message()
        {
            var record = _store.EnumerateMessages("stream4", 0, Int32.MaxValue).First();

            Assert.AreEqual(1, record.Items.Length);
            Assert.AreEqual("msg1", (record.Items[0] as SerializerTest1).Name);
        }

        [Test]
        public void when_append_to_store()
        {
            var store = new MessageStore(_appendOnlyStore, _serializer);
            store.AppendToStore("stream5", MessageAttribute.Empty, -1, new[] { new SerializerTest1 { Name = "name1" } });
            var records = store.EnumerateMessages("stream5", 0, Int32.MaxValue).ToArray();

            Assert.AreEqual(1, records.Length);
            Assert.AreEqual(1, records[0].Items.Length);
            Assert.AreEqual("name1", (records[0].Items[0] as SerializerTest1).Name);
        }
    }
}