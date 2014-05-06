using System;
using System.IO;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class specification_with_empty_directory
    {
        protected string DirectoryPath;

        [SetUp]
        public void Setup()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), "MessageSender", Guid.NewGuid().ToString());
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(DirectoryPath))
                Directory.Delete(DirectoryPath, true);
        } 
    }
}