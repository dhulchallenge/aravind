#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NUnit.Framework;
using ProtoBuf;

namespace Cqrs.Azure.Tests.AtomicStorage
{
    public class AzureAtomicWriterAndReaderTest
    {
        AzureAtomicWriter<Guid, TestView> _writer;
        DocumentStrategy _documentStrategy;
        AzureAtomicReader<Guid, TestView> _reader;
        CloudBlobClient _cloudBlobClient;
        string name;
        private bool _container;
        private CloudBlobContainer _cloudBlobContainer;

        [SetUp]
        public void Setup()
        {
            CloudStorageAccount cloudStorageAccount = ConnectionConfig.StorageAccount;
            name = Guid.NewGuid().ToString().ToLowerInvariant();
            _cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
           _cloudBlobContainer = _cloudBlobClient.GetBlobDirectoryReference(name).Container;
            _cloudBlobContainer.CreateIfNotExist();
            _documentStrategy = new DocumentStrategy(name);
            _writer = new AzureAtomicWriter<Guid, TestView>(_cloudBlobClient, _documentStrategy);
            _reader = new AzureAtomicReader<Guid, TestView>(_cloudBlobClient, _documentStrategy);
        }

        [TearDown]
        public void TearDown()
        {
            _cloudBlobContainer.Delete();
        }

        [Test]
        public void when_delete_than_not_key()
        {
            Assert.IsFalse(_writer.TryDelete(Guid.NewGuid()));
        }

        [Test]
        public void when_delete_than_exist_key()
        {
            _writer.InitializeIfNeeded();
            var id = Guid.NewGuid();
            _writer.AddOrUpdate(id, () => new TestView(id)
                , old =>
                    {
                        old.Data++;
                        return old;
                    }
                , AddOrUpdateHint.ProbablyExists);

            Assert.IsTrue(_writer.TryDelete(id));
        }

        [Test]
        public void when_write_read()
        {
            var id = Guid.NewGuid();
            _writer.AddOrUpdate(id, () => new TestView(id)
                , old =>
                    {
                        old.Data++;
                        return old;
                    }
                , AddOrUpdateHint.ProbablyExists);

            TestView entity;
            var result = _reader.TryGet(id, out entity);

            Assert.IsTrue(result);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual(0, entity.Data);
        }

        [Test]
        public void when_read_nothing_key()
        {
            var id = Guid.NewGuid();

            TestView entity;
            var result = _reader.TryGet(id, out entity);

            Assert.IsFalse(result);
        }

        [Test]
        public void when_write_exist_key_and_read()
        {
            var id = Guid.NewGuid();
            _writer.AddOrUpdate(id, () => new TestView(id)
                , old =>
                {
                    old.Data++;
                    return old;
                }
                , AddOrUpdateHint.ProbablyExists);
            _writer.AddOrUpdate(id, () => new TestView(id)
                , old =>
                {
                    old.Data++;
                    return old;
                }
                , AddOrUpdateHint.ProbablyExists);

            TestView entity;
            var result = _reader.TryGet(id, out entity);

            Assert.IsTrue(result);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual(1, entity.Data);
        }
    }

    [DataContract(Name = "test-view")]
    public class TestView
    {
        [DataMember(Order = 1)]
        public Guid Id { get; set; }
        [DataMember(Order = 2)]
        public int Data { get; set; }

        public TestView()
        { }

        public TestView(Guid id)
        {
            Id = id;
            Data = 0;
        }
    }

    public sealed class DocumentStrategy : IDocumentStrategy
    {
        private string _uniqName;

        public DocumentStrategy(string uniqName)
        {
            _uniqName = uniqName;
        }

        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            // ProtoBuf must have non-zero files
            stream.WriteByte(42);
            Serializer.Serialize(stream, entity);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            var signature = stream.ReadByte();

            if (signature != 42)
                throw new InvalidOperationException("Unknown view format");

            return Serializer.Deserialize<TEntity>(stream);
        }

        public string GetEntityBucket<TEntity>()
        {
            return _uniqName + "/" + NameCache<TEntity>.Name;
        }

        public string GetEntityLocation<TEntity>(object key)
        {
            if (key is unit)
                return NameCache<TEntity>.Name + ".pb";

            return key.ToString().ToLowerInvariant() + ".pb";
        }
    }

    static class NameCache<T>
    {
        // ReSharper disable StaticFieldInGenericType
        public static readonly string Name;
        public static readonly string Namespace;
        // ReSharper restore StaticFieldInGenericType
        static NameCache()
        {
            var type = typeof(T);

            Name = new string(Splice(type.Name).ToArray()).TrimStart('-');
            var dcs = type.GetCustomAttributes(false).OfType<DataContractAttribute>().ToArray();


            if (dcs.Length <= 0) return;
            var attribute = dcs.First();

            if (!string.IsNullOrEmpty(attribute.Name))
            {
                Name = attribute.Name;
            }

            if (!string.IsNullOrEmpty(attribute.Namespace))
            {
                Namespace = attribute.Namespace;
            }
        }

        static IEnumerable<char> Splice(string source)
        {
            foreach (var c in source)
            {
                if (char.IsUpper(c))
                {
                    yield return '-';
                }
                yield return char.ToLower(c);
            }
        }
    }
}