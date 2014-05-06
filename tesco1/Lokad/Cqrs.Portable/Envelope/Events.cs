#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;

namespace Lokad.Cqrs.Envelope.Events
{
    /// <summary>
    /// Raised when something goes wrong with the envelope deserialization (i.e.: unknown format or contract)
    /// </summary>
    [Serializable]
    public sealed class EnvelopeDeserializationFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public string Origin { get; private set; }

        public EnvelopeDeserializationFailed(Exception exception, string origin)
        {
            Exception = exception;
            Origin = origin;
        }


        public override string ToString()
        {
            return string.Format("Failed to deserialize in '{0}': '{1}'", Origin, Exception.ToString());
        }
    }

    /// <summary>
    /// Is published whenever an event is sent.
    /// </summary>
    [Serializable]
    public sealed class EnvelopeSent : ISystemEvent
    {
        public readonly string QueueName;
        public readonly string EnvelopeId;
        public readonly string MappedTypes;
        public readonly ICollection<MessageAttribute> Attributes;

        public EnvelopeSent(string queueName, string envelopeId, string mappedTypes, ICollection<MessageAttribute> attributes)
        {
            QueueName = queueName;
            EnvelopeId = envelopeId;
            MappedTypes = mappedTypes;
            Attributes = attributes;
        }

        public override string ToString()
        {
            return string.Format("Sent {0} to '{1}' as [{2}]",
                MappedTypes,
                QueueName,
                EnvelopeId);
        }
    }
    [Serializable]
    public sealed class EnvelopeQuarantined : ISystemEvent
    {
        public Exception LastException { get; private set; }
        public string Dispatcher { get; private set; }
        public ImmutableEnvelope Envelope { get; private set; }

        public EnvelopeQuarantined(Exception lastException, string dispatcher, ImmutableEnvelope envelope)
        {
            LastException = lastException;
            Dispatcher = dispatcher;
            Envelope = envelope;
        }

        public override string ToString()
        {
            return string.Format("Quarantined '{0}': {1}", Envelope.EnvelopeId, LastException.Message);
        }
    }

    [Serializable]
    public sealed class EnvelopeCleanupFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public string Dispatcher { get; private set; }
        public ImmutableEnvelope Envelope { get; private set; }

        public EnvelopeCleanupFailed(Exception exception, string dispatcher, ImmutableEnvelope envelope)
        {
            Exception = exception;
            Dispatcher = dispatcher;
            Envelope = envelope;
        }
    }

    [Serializable]
    public sealed class EnvelopeDuplicateDiscarded : ISystemEvent
    {
        public string EnvelopeId { get; private set; }

        public EnvelopeDuplicateDiscarded(string envelopeId)
        {
            EnvelopeId = envelopeId;
        }

        public override string ToString()
        {
            return string.Format("[{0}] duplicate discarded", EnvelopeId);
        }
    }
    [Serializable]
    public sealed class EnvelopeDispatched : ISystemEvent
    {
        public ImmutableEnvelope Envelope { get; private set; }
        public string Dispatcher { get; private set; }
        public EnvelopeDispatched(ImmutableEnvelope envelope, string dispatcher)
        {
            Envelope = envelope;
            Dispatcher = dispatcher;
        }

        public override string ToString()
        {
            return string.Format("Envelope '{0}' was dispatched by '{1}'", Envelope.EnvelopeId, Dispatcher);
        }

    }


}