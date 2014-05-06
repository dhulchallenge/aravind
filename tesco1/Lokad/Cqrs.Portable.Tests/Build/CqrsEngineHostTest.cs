using System;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Build
{
    public class CqrsEngineHostTest
    {
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void when_start_task_where_empty_task()
        {
            var host = new CqrsEngineHost(new IEngineProcess[]{});
            host.Start(new CancellationToken());
        }

        [Test]
        public void when_start_task()
        {
            var testEngineProcess = new TestEngineProcess();
            var host = new CqrsEngineHost(new[] { testEngineProcess, });
            var task = host.Start(new CancellationToken());
            task.Wait();

            Assert.IsTrue(testEngineProcess.IsStarted);
        }

        [Test]
        public void when_disposed_task()
        {
            var testEngineProcess = new TestEngineProcess();
            var host = new CqrsEngineHost(new[] { testEngineProcess, });
            host.Dispose();

            Assert.IsTrue(testEngineProcess.IsDisposed);
        }
    }
}