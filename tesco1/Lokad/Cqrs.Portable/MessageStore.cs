using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.TapeStorage;

namespace Lokad.Cqrs
{
    /// <summary>
    /// Helper class that knows how to store arbitrary messages in append-only store
    /// (including envelopes, audit batches etc)
    /// </summary>
    public class MessageStore
    {
        readonly IAppendOnlyStore _appendOnlyStore;
        readonly IMessageSerializer _serializer;

        public void Dispose()
        {
            _appendOnlyStore.Close();
            _appendOnlyStore.Dispose();
        }

        public MessageStore(IAppendOnlyStore appendOnlyStore, IMessageSerializer serializer)
        {
            _appendOnlyStore = appendOnlyStore;
            _serializer = serializer;
        }

        public IEnumerable<StoreRecord> EnumerateMessages(string key, long version, int count)
        {
            var records = _appendOnlyStore.ReadRecords(key, version, count);
            foreach (var record in records)
            {
                using (var mem = new MemoryStream(record.Data))
                {
                    // drop attributes
                    var attribs = _serializer.ReadAttributes(mem);
                    var eventCount = _serializer.ReadCompactInt(mem);
                    var objects = new object[eventCount];
                    for (int i = 0; i < eventCount; i++)
                    {
                        objects[i] = _serializer.ReadMessage(mem);
                    }
                    yield return new StoreRecord(key, objects, record.StoreVersion, record.StreamVersion);
                }
            }
        }

        public long GetVersion()
        {
            return _appendOnlyStore.GetCurrentVersion();
        }


        public IEnumerable<StoreRecord> EnumerateAllItems(long startingFrom, int take)
        {
            // we don't use any index = just skip all audit things
            foreach (var record in _appendOnlyStore.ReadRecords(startingFrom, take))
            {
                using (var mem = new MemoryStream(record.Data))
                {
                    // ignore the attributes here
                    var attribs = _serializer.ReadAttributes(mem);
                    var count = _serializer.ReadCompactInt(mem);
                    var result = new object[count];
                    for (int i = 0; i < count; i++)
                    {
                        result[i] = _serializer.ReadMessage(mem);
                    }
                    yield return new StoreRecord(record.Key, result, record.StoreVersion, record.StreamVersion);
                }
            }
        }

        public void AppendToStore(string name, ICollection<MessageAttribute> attribs, long streamVersion, ICollection<object> messages)
        {
            using (var mem = new MemoryStream())
            {
                _serializer.WriteAttributes(attribs, mem);
                _serializer.WriteCompactInt(messages.Count(), mem);
                foreach (var message in messages)
                {
                    _serializer.WriteMessage(message, message.GetType(), mem);
                }
                _appendOnlyStore.Append(name, mem.ToArray(), streamVersion);
            }
        }

        public void RecordMessage(string key, ImmutableEnvelope envelope)
        {
            // record properties as attributes
            var attribs = new List<MessageAttribute>(envelope.Attributes.Count + 2)
                {
                    new MessageAttribute("id", envelope.EnvelopeId), 
                    new MessageAttribute("utc", envelope.CreatedUtc.ToString("o"))
                };
            // copy existing attributes
            attribs.AddRange(envelope.Attributes);
            AppendToStore(key, attribs, -1, new[] { envelope.Message });
        }
    }
    public struct StoreRecord
    {
        public readonly object[] Items;
        public readonly long StoreVersion;
        public readonly long StreamVersion;
        public readonly string Key;


        public StoreRecord(string key, object[] items, long storeVersion, long streamVersion)
        {
            Items = items;
            StoreVersion = storeVersion;
            StreamVersion = streamVersion;
            Key = key;
        }
    }
}