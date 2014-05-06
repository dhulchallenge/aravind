using System.IO;
using System.Linq;
using System.Text;
using Lokad.Cqrs.StreamingStorage;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.DataStreams
{
    public class FileStreamContainerTest
    {
        private FileStreamContainer _container;
        private string _path;

        [SetUp]
        public void Setup()
        {
            _path = Path.Combine(Path.GetTempPath(), "FileStreamContainerTest");
            _container = new FileStreamContainer(_path);
            _container.Create();
        }

        [TearDown]
        public void TearDown()
        {
            _container.Delete();
        }

        [Test]
        public void when_create_container()
        {
            _container.Create();
            Assert.IsTrue(_container.Exists());
        }

        [Test]
        public void when_delete_created_container()
        {
            _container.Create();
            _container.Delete();

            Assert.IsFalse(_container.Exists());
        }

        [Test]
        public void when_delete_not_created_container()
        {
            if (Directory.Exists(_path))
                Directory.Delete(_path);
            _container.Delete();

            Assert.IsFalse(_container.Exists());
        }

        [Test]
        public void when_exist_container()
        {
            _container.Create();

            Assert.IsTrue(_container.Exists());
        }

        [Test]
        public void when_not_exist_container()
        {
            _container.Delete();

            Assert.IsFalse(_container.Exists());
        }

        [Test]
        public void when_full_path()
        {
            Assert.AreEqual(_path, _container.FullPath);
        }

        [Test]
        public void when_child_container()
        {
            var child = _container.GetContainer("child");

            Assert.AreEqual(Path.Combine(_path, "child"), child.FullPath);
        }

        [Test]
        public void when_list_containers()
        {
            _container.Delete();
            _container.Create();
            _container.GetContainer("container1").Create();
            _container.GetContainer("container2").Create();
            _container.GetContainer("container3").Create();
            var list = _container.ListContainers().ToArray();

            Assert.AreEqual(3, list.Length);
            Assert.AreEqual("container1", list[0]);
            Assert.AreEqual("container2", list[1]);
            Assert.AreEqual("container3", list[2]);
        }

        [Test]
        public void when_list_containers_by_prefix()
        {
            _container.Delete();
            _container.Create();
            _container.GetContainer("container1").Create();
            _container.GetContainer("pref-container2").Create();
            _container.GetContainer("container3").Create();
            var list = _container.ListContainers("pref").ToArray();

            Assert.AreEqual(1, list.Length);
            Assert.AreEqual("pref-container2", list[0]);
        }

        [Test]
        public void when_open_write()
        {
            using (var stream = _container.OpenWrite("test1.dat"))
            {
                var b = Encoding.UTF8.GetBytes("test");
                stream.Write(b, 0, b.Length);
            }

            Assert.IsTrue(_container.Exists("test1.dat"));
        }

        [Test, Ignore("possible to simultaneously read and write")]
        public void when_open_write_and_attempt_read()
        { }

        [Test]
        public void when_open_read()
        {
            var b = Encoding.UTF8.GetBytes("test");
            using (var stream = _container.OpenWrite("test3.dat"))
            {
                stream.Write(b, 0, b.Length);
            }

            var readBytes = new byte[b.Length];
            using (var stream = _container.OpenRead("test3.dat"))
            {
                stream.Read(readBytes, 0, b.Length);
            }

            CollectionAssert.AreEquivalent(b, readBytes);
        }

        [Test]
        public void when_delete()
        {
            var b = Encoding.UTF8.GetBytes("test");
            using (var stream = _container.OpenWrite("test4.dat"))
            {
                stream.Write(b, 0, b.Length);
            }

            _container.TryDelete("test4.dat");

            Assert.IsFalse(_container.Exists("test4.dat"));
        }

        [Test]
        public void when_list_items()
        {
            var b = Encoding.UTF8.GetBytes("test");
            _container.Delete();
            _container.Create();
            using (var stream = _container.OpenWrite("test5.dat"))
                stream.Write(b, 0, b.Length);
            using (var stream = _container.OpenWrite("test6.dat"))
                stream.Write(b, 0, b.Length);

            var items = _container.ListAllNestedItems().ToArray();

            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("test5.dat", items[0]);
            Assert.AreEqual("test6.dat", items[1]);
        }

        [Test, ExpectedException(typeof(StreamContainerNotFoundException))]
        public void when_list_items_where_container_not_created()
        {
            var b = Encoding.UTF8.GetBytes("test");
            _container.Delete();

            _container.ListAllNestedItems().ToArray();
        }

        [Test]
        public void when_list_detail_items()
        {
            var b = Encoding.UTF8.GetBytes("test");
            _container.Delete();
            _container.Create();
            using (var stream = _container.OpenWrite("test5.dat"))
                stream.Write(b, 0, b.Length);
            using (var stream = _container.OpenWrite("test6.dat"))
                stream.Write(b, 0, b.Length);

            var items = _container.ListAllNestedItemsWithDetail().ToArray();

            Assert.AreEqual(2, items.Length);
            Assert.AreEqual("test5.dat", items[0].Name);
            Assert.AreEqual(b.Length, items[0].Length);
            Assert.AreEqual("test6.dat", items[1].Name);
            Assert.AreEqual(b.Length, items[1].Length);
        }

        [Test, ExpectedException(typeof(StreamContainerNotFoundException))]
        public void when_list_detailed_items_where_container_not_created()
        {
            _container.Delete();
            _container.ListAllNestedItemsWithDetail().ToArray();
        }
    }
}