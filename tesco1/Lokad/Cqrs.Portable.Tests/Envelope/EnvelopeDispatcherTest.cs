using System;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using NUnit.Framework;

namespace Cqrs.Portable.Tests.Envelope
{
    public class EnvelopeDispatcherTest
    {
        private TestEnvelopeStreamer _testEnvelopeStreamer;
        private EnvelopeDispatcher _envelopeDispatcher;
        private TestEnvelopeQuarantine _testEnvelopeQuarantine;
        private DuplicationManager _duplicationManager;
        private bool ActionCalled;

        [SetUp]
        public void SetUp()
        {
            _testEnvelopeStreamer = new TestEnvelopeStreamer(new byte[] { 1, 2, 3 });
            _testEnvelopeQuarantine = new TestEnvelopeQuarantine();
            _duplicationManager = new DuplicationManager();
            _envelopeDispatcher = new EnvelopeDispatcher(e => { ActionCalled = true; }, _testEnvelopeStreamer, _testEnvelopeQuarantine,
                                                         _duplicationManager, "D1");
        }

        [Test]
        public void when_dispatch_null_message()
        {
            ActionCalled = false;
            _envelopeDispatcher.Dispatch(null);

            Assert.IsFalse(ActionCalled);
            Assert.IsTrue(_testEnvelopeQuarantine.CallQuarantineMethod);
            Assert.AreEqual(typeof(ArgumentNullException), _testEnvelopeQuarantine.Exception.GetType());
        }

        [Test]
        public void when_dispatch_dublicate_message()
        {
            ActionCalled = false;
            var dublicationMemeory = _duplicationManager.GetOrAdd(_envelopeDispatcher);
            dublicationMemeory.Memorize("EnvId");

            _envelopeDispatcher.Dispatch(Encoding.UTF8.GetBytes("test queue"));

            Assert.IsFalse(ActionCalled);
            Assert.IsFalse(_testEnvelopeQuarantine.CallQuarantineMethod);
        }

        [Test]
        public void when_dispatch_call_action_message()
        {
            ActionCalled = false;
            _envelopeDispatcher.Dispatch(Encoding.UTF8.GetBytes("test queue"));
            var dublicationMemeory = _duplicationManager.GetOrAdd(_envelopeDispatcher);

            Assert.IsTrue(ActionCalled);
            Assert.IsFalse(_testEnvelopeQuarantine.CallQuarantineMethod);
            Assert.IsTrue(dublicationMemeory.DoWeRemember("EnvId"));
        }
    }

    public class TestEnvelopeQuarantine : IEnvelopeQuarantine
    {
        public byte[] Message { get; set; }
        public Exception Exception { get; set; }
        public bool CallQuarantineMethod { get; set; }

        public bool TryToQuarantine(ImmutableEnvelope optionalEnvelope, Exception ex)
        {
            return true;
        }

        public void Quarantine(byte[] message, Exception ex)
        {
            Message = message;
            Exception = ex;
            CallQuarantineMethod = true;
        }

        public void TryRelease(ImmutableEnvelope context)
        {

        }
    }

    public class TestEnvelopeStreamer : IEnvelopeStreamer
    {
        public ImmutableEnvelope Envelope { get; private set; }
        public byte[] Buffer { get; set; }

        public TestEnvelopeStreamer()
        { }

        public TestEnvelopeStreamer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte[] SaveEnvelopeData(ImmutableEnvelope envelope)
        {
            Envelope = envelope;
            Buffer = new byte[] { 1, 2, 3 };

            return Buffer;
        }

        public ImmutableEnvelope ReadAsEnvelopeData(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException();
            return new ImmutableEnvelope("EnvId", DateTime.UtcNow, "Test meesage", new[] { new MessageAttribute("key", "value"), });
        }
    }

    public class TestQueueWriter : IQueueWriter
    {
        public byte[] Envelope { get; private set; }

        public string Name { get { return "TestQueueWriter"; } }
        public void PutMessage(byte[] envelope)
        {
            Envelope = envelope;
        }
    }
}