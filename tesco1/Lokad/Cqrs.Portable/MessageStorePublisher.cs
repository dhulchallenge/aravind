using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using Lokad.Cqrs.AtomicStorage;
using System.Linq;

namespace Lokad.Cqrs
{
    /// <summary>
    /// Is responsible for publishing events from the event store
    /// </summary>
    public sealed class MessageStorePublisher
    {
        readonly MessageStore _store;
        readonly MessageSender _sender;
        readonly NuclearStorage _storage;
        readonly Predicate<StoreRecord> _recordShouldBePublished;

        public MessageStorePublisher(MessageStore store, MessageSender sender, NuclearStorage storage, Predicate<StoreRecord> recordShouldBePublished)
        {
            _store = store;
            _sender = sender;
            _storage = storage;
            _recordShouldBePublished = recordShouldBePublished;
        }

        public sealed class PublishResult
        {
            public readonly long InitialPosition;
            public readonly long FinalPosition;
            public readonly bool Changed;
            public readonly bool HasMoreWork;

            public PublishResult(long initialPosition, long finalPosition, int requestedBatchSize)
            {
                InitialPosition = initialPosition;
                FinalPosition = finalPosition;


                Changed = InitialPosition != FinalPosition;
                // thanks to Slav Ivanyuk for fixing finding this typo
                HasMoreWork = (FinalPosition - InitialPosition) >= requestedBatchSize;
            }
        }

        PublishResult PublishEventsIfAnyNew(long initialPosition, int count)
        {
            var records = _store.EnumerateAllItems(initialPosition, count);
            var currentPosition = initialPosition;
            var publishedCount = 0;
            foreach (var e in records)
            {
                if (e.StoreVersion < currentPosition)
                {
                    throw new InvalidOperationException(string.Format("Retrieved record with position less than current. Store versions {0} <= current position {1}", e.StoreVersion, currentPosition));
                }
                if (_recordShouldBePublished(e))
                {
                    for (int i = 0; i < e.Items.Length; i++)
                    {
                        // predetermined id to kick in event deduplication
                        // if server crashes somehow
                        var envelopeId = "esp-" + e.StoreVersion + "-" + i;
                        var item = e.Items[i];

                        publishedCount += 1;
                        _sender.Send(item, envelopeId);
                    }
                }
                currentPosition = e.StoreVersion;
            }
            var result = new PublishResult(initialPosition, currentPosition, count);
            if (result.Changed)
            {
                SystemObserver.Notify("[sys ] Message store pointer moved to {0} ({1} published)", result.FinalPosition, publishedCount);
            }
            return result;
        }

        public void Run(CancellationToken token)
        {
            long? currentPosition = null;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // reinitialize state from persistent store, if absent
                    if (currentPosition == null)
                    {
                        // if we fail here, we'll get into retry
                        currentPosition = _storage.GetSingletonOrNew<PublishCounter>().Position;
                    }
                    // publish events, if any
                    var publishResult = PublishEventsIfAnyNew(currentPosition.Value, 25);
                    if (publishResult.Changed)
                    {
                        // ok, we are changed, persist that to survive crashes
                        var output = _storage.UpdateSingletonEnforcingNew<PublishCounter>(c =>
                        {
                            if (c.Position != publishResult.InitialPosition)
                            {
                                throw new InvalidOperationException("Somebody wrote in parallel. Blow up!");
                            }
                            // we are good - update ES
                            c.Position = publishResult.FinalPosition;

                        });
                        currentPosition = output.Position;
                    }
                    if (!publishResult.HasMoreWork)
                    {
                        // wait for a few ms before polling ES again
                        token.WaitHandle.WaitOne(400);
                    }
                }
                catch (Exception ex)
                {
                    // we messed up, roll back
                    currentPosition = null;
                    Trace.WriteLine(ex);
                    token.WaitHandle.WaitOne(5000);
                }
            }
        }

        public void VerifyEventStreamSanity()
        {
            var result = _storage.GetSingletonOrNew<PublishCounter>();
            if (result.Position != 0)
            {
                SystemObserver.Notify("Continuing work with existing event store");
                return;
            }
            var store = _store.EnumerateAllItems(0, 100).ToArray();
            if (store.Length == 0)
            {
                SystemObserver.Notify("Opening new event stream");
                //_sender.SendHashed(new EventStreamStarted());
                return;
            }
            if (store.Length == 100)
            {
                throw new InvalidOperationException(
                    "It looks like event stream really went ahead (or storage pointer was reset). Do you REALLY mean to resend all events?");
            }
        }

        /// <summary>  Storage contract used to persist current position  </summary>
        [DataContract]
        public sealed class PublishCounter
        {
            [DataMember(Order = 1)]
            public long Position { get; set; }
        }
    }
}