#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Security.Cryptography;
using Lokad.Cqrs.Envelope.Events;
using Lokad.Cqrs.Partition;

namespace Lokad.Cqrs
{
    /// <summary>
    /// Allows sending serializable messages over the wire
    /// </summary>
    public sealed class MessageSender 
    {
        readonly IQueueWriter _queue;
        readonly IEnvelopeStreamer _streamer;

        public MessageSender(IEnvelopeStreamer streamer, IQueueWriter queue)
        {
            _queue = queue;
            _streamer = streamer;
        }

        public void SendHashed(object message, params MessageAttribute[] attributes)
        {
            var envelopeId = GenerateSha1HashFromContent(message, attributes);
            Send(message, envelopeId,attributes);
        }

        public void Send(object message, params MessageAttribute[] attributes)
        {
            var envelopeId = Guid.NewGuid().ToString().ToLowerInvariant();
            Send(message, envelopeId, attributes);
        }

        public void Send(object message, string envelopeId, params MessageAttribute[] attributes)
        {
            var envelope = new ImmutableEnvelope(envelopeId, DateTime.UtcNow, message, attributes);
            var data = _streamer.SaveEnvelopeData(envelope);

            _queue.PutMessage(data);

            SystemObserver.Notify(new EnvelopeSent(
                _queue.Name, 
                envelope.EnvelopeId,
                envelope.Message.GetType().Name, 
                envelope.Attributes));
        }


        string GenerateSha1HashFromContent(object message, MessageAttribute[] attributes)
        {
            // we need to set ID and date to fixed
            var envelope = new ImmutableEnvelope("", default(DateTime), message, attributes);
            var data = _streamer.SaveEnvelopeData(envelope);
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}