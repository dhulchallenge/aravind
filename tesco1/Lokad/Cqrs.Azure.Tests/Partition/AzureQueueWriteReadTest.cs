#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Text;
using System.Threading;
using Lokad.Cqrs.Feature.AzurePartition;
using Lokad.Cqrs.Feature.AzurePartition.Inbox;
using Lokad.Cqrs.Partition;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NUnit.Framework;

namespace Cqrs.Azure.Tests.Partition
{
    public class AzureQueueWriteReadTest
    {
        StatelessAzureQueueReader _statelessReader;
        AzureQueueReader _queueReader;
        StatelessAzureQueueWriter _queueWriter;
        string _name;
        private CloudBlobClient _cloudBlobClient;
        private CloudBlobContainer _blobContainer;


        [SetUp]
        public void Setup()
        {
            _name = Guid.NewGuid().ToString().ToLowerInvariant();
            CloudStorageAccount cloudStorageAccount = ConnectionConfig.StorageAccount;

            _cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var queue = cloudStorageAccount.CreateCloudQueueClient().GetQueueReference(_name);
            var container = _cloudBlobClient.GetBlobDirectoryReference(_name);

            _blobContainer = _cloudBlobClient.GetContainerReference(_name);
            var poisonQueue = new Lazy<CloudQueue>(() =>
            {
                var queueReference = cloudStorageAccount.CreateCloudQueueClient().GetQueueReference(_name + "-poison");
                queueReference.CreateIfNotExist();
                return queueReference;
            }, LazyThreadSafetyMode.ExecutionAndPublication);
            _statelessReader = new StatelessAzureQueueReader("azure-read-write-message", queue, container, poisonQueue, TimeSpan.FromMinutes(1));
            _queueReader = new AzureQueueReader(new[] { _statelessReader }, x => TimeSpan.FromMinutes(x));
            _queueWriter = new StatelessAzureQueueWriter(_blobContainer, queue, "azure-read-write-message");
            _queueWriter.Init();
        }


        [TearDown]
        public void Teardown()
        {
            _blobContainer.Delete();
        }

        [Test, ExpectedException(typeof(NullReferenceException))]
        public void when_put_null_message()
        {
            _queueWriter.PutMessage(null);
        }

        [Test]
        public void when_get_added_message()
        {
            _queueWriter.PutMessage(Encoding.UTF8.GetBytes("message"));
            var msg = _statelessReader.TryGetMessage();

            Assert.AreEqual(GetEnvelopeResultState.Success, msg.State);
            Assert.AreEqual("message", Encoding.UTF8.GetString(msg.Message.Unpacked));
            Assert.AreEqual("azure-read-write-message", msg.Message.QueueName);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void when_ack_null_message()
        {
            _statelessReader.AckMessage(null);
        }

        [Test]
        public void when_ack_messages_by_name()
        {
            _queueWriter.PutMessage(Encoding.UTF8.GetBytes("message"));
            var msg = _statelessReader.TryGetMessage();

            var transportContext = new MessageTransportContext(
                msg.Message.TransportMessage
                , msg.Message.Unpacked
                , msg.Message.QueueName);
            _statelessReader.AckMessage(transportContext);
            var msg2 = _statelessReader.TryGetMessage();

            Assert.AreEqual(GetEnvelopeResultState.Empty, msg2.State);
        }

        [Test]
        public void when_reader_ack_messages()
        {
            _queueWriter.PutMessage(Encoding.UTF8.GetBytes("message"));
            var msg = _statelessReader.TryGetMessage();

            var transportContext = new MessageTransportContext(
                msg.Message.TransportMessage
                , msg.Message.Unpacked
                , msg.Message.QueueName);

            _queueReader.AckMessage(transportContext);
            var msg2 = _statelessReader.TryGetMessage();

            Assert.AreEqual(GetEnvelopeResultState.Empty, msg2.State);
        }

        [Test]
        public void when_reader_take_messages()
        {
            _queueWriter.PutMessage(Encoding.UTF8.GetBytes("message"));
            MessageTransportContext msg;
            var cancellationToken = new CancellationToken(false);
            var result = _queueReader.TakeMessage(cancellationToken, out msg);

            Assert.IsTrue(result);
            Assert.AreEqual("message", Encoding.UTF8.GetString(msg.Unpacked));
            Assert.AreEqual("azure-read-write-message", msg.QueueName);
        }
    }
}