using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lokad.Cqrs;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class StorageFramesEvilTest
    {
        [Test]
        public void read_write_frame()
        {
            string msg = "test message";

            Stream stream = new MemoryStream();
            StorageFramesEvil.WriteFrame("test-key", 555, Encoding.UTF8.GetBytes(msg), stream);
            stream.Seek(0, SeekOrigin.Begin);
            var decoded = StorageFramesEvil.ReadFrame(stream);

            Assert.AreEqual("test-key", decoded.Name);
            Assert.AreEqual(555, decoded.Stamp);
            Assert.AreEqual(msg, Encoding.UTF8.GetString(decoded.Bytes));
        }

        [Test]
        public void read_empty_frame()
        {
            Stream stream = new MemoryStream();
            StorageFrameDecoded decoded;
            var isreadFrame = StorageFramesEvil.TryReadFrame(stream, out decoded);

            Assert.IsFalse(isreadFrame);
        }

        [Test]
        public void async_read_write_more_frame()
        {
            //GIVEN
            const string msg = "test message";
            var path = Path.Combine(Path.GetTempPath(), "lokad-cqrs", Guid.NewGuid() + ".pb");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            const int maxIndex = 100;
            var writeTask = Task.Factory.StartNew(() =>
                                       {
                                           using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                                           {
                                               for (int i = 0; i < maxIndex; i++)
                                               {
                                                   StorageFramesEvil.WriteFrame("test-key" + i, i, Encoding.UTF8.GetBytes(msg + i), stream);
                                               }
                                           }
                                       });


            //THEN
            int index = 0;
            var readTask = Task.Factory.StartNew(() =>
              {
                  using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                  {
                      while (index < maxIndex)
                      {
                          StorageFrameDecoded decoded;
                          if (StorageFramesEvil.TryReadFrame(stream, out decoded))
                          {
                              Assert.AreEqual("test-key" + index, decoded.Name);
                              Assert.AreEqual(index, decoded.Stamp);
                              Assert.AreEqual(msg + index, Encoding.UTF8.GetString(decoded.Bytes));
                              index++;
                          }
                      }
                  }
              });

            writeTask.Wait();
            readTask.Wait();

            Assert.AreEqual(maxIndex, index);
        }
    }
}