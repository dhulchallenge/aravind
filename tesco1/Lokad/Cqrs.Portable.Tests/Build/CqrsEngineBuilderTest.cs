using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Dispatch;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Build
{
    public class CqrsEngineBuilderTest
    {
        [Test]
        public void when_create_instance()
        {
            var builder = new CqrsEngineBuilder(null);

            Assert.AreEqual(1, builder.Processes.Count);
            Assert.AreEqual(typeof(DuplicationManager), builder.Processes[0].GetType());
        }

        [Test]
        public void when_add_process()
        {
            var builder = new CqrsEngineBuilder(null);
            var testEngineProcess = new TestEngineProcess();
            builder.AddTask(testEngineProcess);

            Assert.AreEqual(2, builder.Processes.Count);
            Assert.AreEqual(testEngineProcess, builder.Processes[1]);
        }

        [Test]
        public void when_add_factory_to_start_process()
        {
            var builder = new CqrsEngineBuilder(null);
            builder.AddTask(x => new Task(() => { }, x));

            Assert.AreEqual(2, builder.Processes.Count);
            Assert.AreEqual(typeof(TaskProcess), builder.Processes[1].GetType());
        }

        [Test]
        public void when_dispatch()
        {
            var builder = new CqrsEngineBuilder(null);
            builder.Dispatch(new TestQueueReader(), (b) => { });

            Assert.AreEqual(2, builder.Processes.Count);
            Assert.AreEqual(typeof(DispatcherProcess), builder.Processes[1].GetType());
        }

        [Test]
        public void when_handle()
        {
            var builder = new CqrsEngineBuilder(null);
            builder.Dispatch(new TestQueueReader(), (b) => { });

            Assert.AreEqual(2, builder.Processes.Count);
            Assert.AreEqual(typeof(DispatcherProcess), builder.Processes[1].GetType());
        }

        [Test]
        public void when_build()
        {
            var builder = new CqrsEngineBuilder(null);
            var testEngineProcess = new TestEngineProcess();
            builder.AddTask(testEngineProcess);
            builder.Build(new CancellationToken());

            Assert.IsTrue(testEngineProcess.IsInitialized);
        }
    }

    public class TestEngineProcess : IEngineProcess
    {
        public bool IsInitialized { get; set; }
        public bool IsStarted { get; set; }
        public bool IsDisposed { get; set; }
        public void Dispose()
        {
            IsDisposed = true;
        }

        public void Initialize(CancellationToken token)
        {
            IsInitialized = true;
        }

        public Task Start(CancellationToken token)
        {
            var start = new Task(() => { IsStarted = true; });
            start.Start();
            return start;
        }
    }

    public class TestQueueReader : IQueueReader
    {
        public void InitIfNeeded()
        {
        }

        public void AckMessage(MessageTransportContext message)
        {
        }

        public bool TakeMessage(CancellationToken token, out MessageTransportContext context)
        {
            context = new MessageTransportContext(null, new byte[0], "Name");
            return false;
        }

        public void TryNotifyNack(MessageTransportContext context)
        {
        }
    }
}