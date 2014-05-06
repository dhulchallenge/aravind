using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cqrs.Portable.Tests.Envelope;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.TapeStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class MessageStorePublisherTest
    {
        private string _path;
        private FileAppendOnlyStore _appendOnlyStore;
        IMessageSerializer _serializer;
        private MessageStore _store;
        private MessageSender _sender;
        private NuclearStorage _nuclearStorage;
        private MessageStorePublisher _publisher;
        static List<StoreRecord> _storeRecords;
        [SetUp]
        public void SetUp()
        {
            _storeRecords = new List<StoreRecord>();
            _serializer = new TestMessageSerializer(new[] { typeof(SerializerTest1), typeof(SerializerTest2), typeof(string) });
            _path = Path.Combine(Path.GetTempPath(), "MessageStorePublisher", Guid.NewGuid().ToString());
            _appendOnlyStore = new FileAppendOnlyStore(new DirectoryInfo(_path));
            _appendOnlyStore.Initialize();
            _store = new MessageStore(_appendOnlyStore, _serializer);
            var streamer = new EnvelopeStreamer(_serializer);
            var queueWriter = new TestQueueWriter();
            _sender = new MessageSender(streamer, queueWriter);
            var store = new FileDocumentStore(Path.Combine(_path, "lokad-cqrs-test"), new DocumentStrategy());
            _nuclearStorage = new NuclearStorage(store);

            _publisher = new MessageStorePublisher(_store, _sender, _nuclearStorage, DoWePublishThisRecord);
        }

        [TearDown]
        public void Teardown()
        {
            _appendOnlyStore.Close();
            Directory.Delete(_path, true);
        }

        static bool DoWePublishThisRecord(StoreRecord storeRecord)
        {
            var result = storeRecord.Key != "audit";
            if (result)
            {
                _storeRecords.Add(storeRecord);
            }
            return result;
        }

        [Test]
        public void when_verify_events_where_empty_store()
        {
            _publisher.VerifyEventStreamSanity();
            var records = _store.EnumerateAllItems(0, 100).ToArray();

            Assert.AreEqual(0, records.Length);
        }

        [Test]
        public void when_verify_events_where_events_count_less_100()
        {
            _store.AppendToStore("stream1", new List<MessageAttribute>(), 0, new List<object> { new SerializerTest1("message1") });
            _store.AppendToStore("stream1", new List<MessageAttribute>(), 1, new List<object> { new SerializerTest1("message1") });

            var records = _store.EnumerateAllItems(0, 100).ToArray();
            _publisher.VerifyEventStreamSanity();

            Assert.AreEqual(2, records.Length);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void when_verify_events_where_events_count_more_100()
        {
            for (int i = 0; i < 101; i++)
                _store.AppendToStore("stream1", new List<MessageAttribute>(), i, new List<object> { new SerializerTest1("message1") });

            var records = _store.EnumerateAllItems(0, 1000).ToArray();

            Assert.AreEqual(101, records.Length);
            _publisher.VerifyEventStreamSanity();
        }

        [Test]
        public void when_run()
        {
            for (int i = 0; i < 50; i++)
                _store.AppendToStore("stream1", new List<MessageAttribute>(), i, new List<object> { new SerializerTest1("message"+i) });

            var cancellationToken = new CancellationToken();

            ThreadPool.QueueUserWorkItem(state => _publisher.Run(cancellationToken));
            while (_storeRecords.Count < 50)
                cancellationToken.WaitHandle.WaitOne(10);

            foreach (StoreRecord storeRecord in _storeRecords)
            {
                Assert.AreEqual("stream1",storeRecord.Key);
                Assert.AreEqual(1, storeRecord.Items.Length);
                Assert.AreEqual(typeof(SerializerTest1), storeRecord.Items[0].GetType());
            }
        }
    }
}