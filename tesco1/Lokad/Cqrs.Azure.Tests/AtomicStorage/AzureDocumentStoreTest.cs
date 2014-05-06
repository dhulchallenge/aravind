#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NUnit.Framework;

namespace Cqrs.Azure.Tests.AtomicStorage
{
    public class AzureDocumentStoreTest
    {
        IDocumentStore _store;
        string _name;
        private CloudBlobContainer _container;
        private CloudBlobContainer _sampleDocContainer;

        [SetUp]
        public void Setup()
        {
            _name = Guid.NewGuid().ToString().ToLowerInvariant();
            CloudStorageAccount cloudStorageAccount = ConnectionConfig.StorageAccount;
            var cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            var documentStrategy = new DocumentStrategy(_name);
            _store = new AzureDocumentStore(documentStrategy, cloudBlobClient);

            _container = cloudBlobClient.GetBlobDirectoryReference(_name).Container;
            _container.CreateIfNotExist();

            _sampleDocContainer = cloudBlobClient.GetBlobDirectoryReference(_name).Container;
            _sampleDocContainer.CreateIfNotExist();
        }

        [TearDown]
        public void Teardown()
        {
            _container.Delete();
            _sampleDocContainer.Delete();
        }

        [Test]
        public void when_get_not_created_bucket()
        {
            //GIVEN
            var bucket = Guid.NewGuid().ToString();

            //WHEN
            CollectionAssert.IsEmpty(_store.EnumerateContents(bucket));
        }


        [Test]
        public void when_write_bucket()
        {
            //GIVEN
            var records = new List<DocumentRecord>
                {
                    new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                    new DocumentRecord("second", () => Encoding.UTF8.GetBytes("test message 2")),
                };
            _store.WriteContents(_name, records);

            //WHEN
            var actualRecords = _store.EnumerateContents(_name).ToList();
            Assert.AreEqual(records.Count, actualRecords.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.AreEqual(true, actualRecords.Count(x => x.Key == records[i].Key) == 1);
                Assert.AreEqual(Encoding.UTF8.GetString(records[i].Read()),
                                Encoding.UTF8.GetString(actualRecords.First(x => x.Key == records[i].Key).Read()));
            }
        }

        [Test]
        public void when_reset_bucket()
        {
            //GIVEN
            var records = new List<DocumentRecord>
                {
                    new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                };
            _store.WriteContents(_name, records);
            _store.Reset(_name);

            //WHEN
            var result1 = _store.EnumerateContents(_name).ToList();
            CollectionAssert.IsEmpty(result1);
        }

        [Test]
        public void when_read_exist_entity()
        {
            var writer = _store.GetWriter<Guid, TestView>();
            var reader = _store.GetReader<Guid, TestView>();
            Guid key = Guid.NewGuid();
            var entity = writer.Add(key, new TestView(key));
            TestView savedView;
            var result = reader.TryGet(key, out savedView);

            Assert.IsTrue(result);
            Assert.AreEqual(key, entity.Id);
            Assert.AreEqual(key, savedView.Id);
        }

        [Test]
        public void when_read_nothing_entity()
        {
            var reader = _store.GetReader<Guid, TestView>();
            Guid key = Guid.NewGuid();
            TestView savedView;
            var result = reader.TryGet(key, out savedView);

            Assert.IsFalse(result);
        }
    }
}