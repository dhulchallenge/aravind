using System;
using System.IO;
using Cqrs.Portable.Tests.Envelope;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Envelope;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class FileStorageConfigTest
    {
        private FileStorageConfig _config;
        private string _configPath;

        [SetUp]
        public void Setup()
        {
            var tmpPath = Path.GetTempPath();
            _configPath = Path.Combine(tmpPath, "lokad-cqrs-test", Guid.NewGuid().ToString());
            _config = new FileStorageConfig(new DirectoryInfo(_configPath), "testaccount");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_configPath))
                Directory.Delete(_configPath, true);
        }

        [Test]
        public void create_config()
        {
            Assert.AreEqual(_configPath, _config.Folder.FullName);
            Assert.AreEqual("testaccount", _config.AccountName);
        }

        [Test]
        public void create_directory()
        {
            if (Directory.Exists(_configPath))
                Directory.Delete(_configPath, true);
            _config.EnsureDirectory();

            Assert.AreEqual(true, Directory.Exists(_configPath));
        }

        [Test]
        public void wipe_directory()
        {
            _config.EnsureDirectory();
            _config.Wipe();

            Assert.AreEqual(false, Directory.Exists(_configPath));
        }

        [Test]
        public void reset_when_folder_exist()
        {
            _config.EnsureDirectory();
            _config.Reset();

            Assert.AreEqual(true, Directory.Exists(_configPath));
        }

        [Test]
        public void reset_when_folder_not_exist()
        {
            _config.Wipe();
            _config.Reset();

            Assert.AreEqual(true, Directory.Exists(_configPath));
        }

        [Test]
        public void create_sub_config_with_new_account()
        {
            var subConfig = _config.SubFolder("sub", "newtestaccount");

            Assert.AreEqual("newtestaccount", subConfig.AccountName);
            Assert.AreEqual(Path.Combine(_configPath, "sub"), subConfig.FullPath);
        }

        [Test]
        public void create_sub_config_with_old_account()
        {
            var subConfig = _config.SubFolder("sub");

            Assert.AreEqual("testaccount-sub", subConfig.AccountName);
            Assert.AreEqual(Path.Combine(_configPath, "sub"), subConfig.FullPath);
        }

        [Test]
        public void wipe_config_when_created_sub_config()
        {
            _config.EnsureDirectory();
            var subConfig = _config.SubFolder("sub", "subaccount");
            subConfig.EnsureDirectory();
            _config.Wipe();

            Assert.AreEqual(false, Directory.Exists(_configPath));
        }

        [Test]
        public void create_document_store()
        {
            var documentStrategy = new DocumentStrategy();
            var store = _config.CreateDocumentStore(documentStrategy);

            Assert.IsTrue(store is FileDocumentStore);
            Assert.AreEqual(new Uri(Path.GetFullPath(_configPath)).AbsolutePath, store.ToString());
            Assert.AreEqual(documentStrategy, store.Strategy);
        }

        [Test]
        public void create_config_and_reset()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(path, "testaccount", true);

            Assert.AreEqual(path, config.FullPath);
            Assert.IsTrue(Directory.Exists(path));
            Assert.AreEqual("testaccount", config.AccountName);
        }

        [Test]
        public void create_config_and_not_reset()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(path, "testaccount", false);

            Assert.AreEqual(path, config.FullPath);
            Assert.IsFalse(Directory.Exists(path));
            Assert.AreEqual("testaccount", config.AccountName);
        }

        [Test]
        public void create_config_with_no_account_name()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(path);

            Assert.AreEqual(path, config.FullPath);
            Assert.IsFalse(Directory.Exists(path));
            Assert.AreEqual(new DirectoryInfo(path).Name, config.AccountName);
        }

        [Test]
        public void create_config_with_directory_info()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(new DirectoryInfo(path));

            Assert.AreEqual(path, config.FullPath);
            Assert.IsFalse(Directory.Exists(path));
            Assert.AreEqual(new DirectoryInfo(path).Name, config.AccountName);
        }

        [Test]
        public void create_streaming()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(path);
            var container = config.CreateStreaming();

            Assert.IsTrue(Directory.Exists(path));
            CollectionAssert.IsEmpty(container.ListContainers());
        }

        [Test]
        public void when_create_child_streaming()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(path);
            var container = config.CreateStreaming("child");

            Assert.IsTrue(Directory.Exists(path));
            Assert.IsTrue(Directory.Exists(Path.Combine(path, "child")));
            Assert.IsTrue(container.Exists());
        }

        [Test]
        public void create_inbox()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(new DirectoryInfo(path));
            var inbox = config.CreateInbox("inbox name", x => new TimeSpan(x));

            Assert.IsNotNull(inbox);
        }

        [Test]
        public void create_QueueWriter()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(new DirectoryInfo(path));
            var queueWriter = config.CreateQueueWriter("QueueName");

            Assert.IsNotNull(queueWriter);
            Assert.AreEqual("QueueName", queueWriter.Name);
            Assert.IsTrue(Directory.Exists(path));
        }

        [Test]
        public void create_AppendOnlyStore()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(new DirectoryInfo(path));
            var appendOnlyStore = config.CreateAppendOnlyStore("append_only");

            Assert.IsNotNull(appendOnlyStore);
        }

        [Test]
        public void create_MessageSender()
        {
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs-test", Guid.NewGuid().ToString());
            var config = FileStorage.CreateConfig(new DirectoryInfo(path));
            var serializer = new TestMessageSerializer(new[] { typeof(SerializerTest1), typeof(SerializerTest2) });
            var streamer = new EnvelopeStreamer(serializer);
            var sender = config.CreateMessageSender(streamer, "QueueName");

            Assert.IsNotNull(sender);
        }
    }
}