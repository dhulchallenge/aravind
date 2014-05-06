using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.AtomicStorage
{
    public class FileDocumentStoreTest : DocumentStoreTest
    {
        [SetUp]
        public void Setup()
        {
            var tmpPath = Path.GetTempPath();
            Store = new FileDocumentStore(Path.Combine(tmpPath, "lokad-cqrs-test"), new DocumentStrategy());
        }

        [Test]
        public void reset_all_bucket()
        {
            //GIVEN
            var bucket1 = "test-bucket1";
            var bucket2 = "test-bucket2";
            var records = new List<DocumentRecord>
                                      {
                                          new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                                      };
            Store.WriteContents(bucket1, records);
            Store.WriteContents(bucket2, records);
            ((FileDocumentStore)Store).ResetAll();

            //WHEN
            var result1 = Store.EnumerateContents(bucket1).ToList();
            var result2 = Store.EnumerateContents(bucket2).ToList();
            CollectionAssert.IsEmpty(result1);
            CollectionAssert.IsEmpty(result2);
        }
    }

    public class MemoryDocumentStoreTest : DocumentStoreTest
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>> _storeDictionary;

        [SetUp]
        public void Setup()
        {
            _storeDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte[]>>();
            Store = new MemoryDocumentStore(_storeDictionary, new DocumentStrategy());
        }

        [Test]
        public void reset_all_bucket()
        {
            //GIVEN
            var bucket1 = "test-bucket1";
            var bucket2 = "test-bucket2";
            var records = new List<DocumentRecord>
                                      {
                                          new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                                      };
            Store.WriteContents(bucket1, records);
            Store.WriteContents(bucket2, records);
            ((MemoryDocumentStore)Store).ResetAll();

            //WHEN
            var result1 = Store.EnumerateContents(bucket1).ToList();
            var result2 = Store.EnumerateContents(bucket2).ToList();
            CollectionAssert.IsEmpty(result1);
            CollectionAssert.IsEmpty(result2);
        }
    }

    public abstract class DocumentStoreTest
    {
        public IDocumentStore Store;

        [Test]
        public void get_not_created_bucket()
        {
            //GIVEN
            var bucket = Guid.NewGuid().ToString();

            //WHEN
            CollectionAssert.IsEmpty(Store.EnumerateContents(bucket));
        }


        [Test]
        public void write_bucket()
        {
            //GIVEN
            var bucket = "test-bucket";
            var records = new List<DocumentRecord>
                                      {
                                          new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                                          new DocumentRecord("second", () => Encoding.UTF8.GetBytes("test message 2")),
                                      };
            Store.WriteContents(bucket, records);

            //WHEN
            var actualRecords = Store.EnumerateContents(bucket).ToList();
            Assert.AreEqual(records.Count, actualRecords.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.AreEqual(true, actualRecords.Count(x => x.Key == records[i].Key) == 1);
                Assert.AreEqual(Encoding.UTF8.GetString(records[i].Read()), Encoding.UTF8.GetString(actualRecords.First(x => x.Key == records[i].Key).Read()));
            }
        }

        [Test]
        public void reset_bucket()
        {
            //GIVEN
            var bucket1 = "test-bucket1";
            var bucket2 = "test-bucket2";
            var records = new List<DocumentRecord>
                                      {
                                          new DocumentRecord("first", () => Encoding.UTF8.GetBytes("test message 1")),
                                      };
            Store.WriteContents(bucket1, records);
            Store.WriteContents(bucket2, records);
            Store.Reset(bucket1);

            //WHEN
            var result1 = Store.EnumerateContents(bucket1).ToList();
            var result2 = Store.EnumerateContents(bucket2).ToList();
            CollectionAssert.IsEmpty(result1);
            Assert.AreEqual(records.Count, result2.Count);
        }
    }
}