#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;

namespace Lokad.Cqrs.Partition
{
    /// <summary>
    /// Describes retrieved message along with the queue name and some transport info.
    /// </summary>
    /// <remarks>It is used to send ACK/NACK back to the originating queue.</remarks>
    public sealed class MessageTransportContext
    {
        public readonly object TransportMessage;
        public readonly byte[] Unpacked;
        public readonly string QueueName;

        public MessageTransportContext(object transportMessage, byte[] unpacked, string queueName)
        {
            TransportMessage = transportMessage;
            QueueName = queueName;
            Unpacked = unpacked;
        }
    }
    public sealed class GetEnvelopeResult
    {
        public static readonly GetEnvelopeResult Empty = new GetEnvelopeResult(null, GetEnvelopeResultState.Empty);
        public static readonly GetEnvelopeResult Retry = new GetEnvelopeResult(null, GetEnvelopeResultState.Retry);
        public readonly GetEnvelopeResultState State;
        readonly MessageTransportContext _message;

        GetEnvelopeResult(MessageTransportContext message, GetEnvelopeResultState state)
        {
            _message = message;
            State = state;
        }


        public MessageTransportContext Message
        {
            get
            {
                if (State != GetEnvelopeResultState.Success)
                    throw new InvalidOperationException("State should be in success");
                return _message;
            }
        }

        public static GetEnvelopeResult Success(MessageTransportContext message)
        {
            return new GetEnvelopeResult(message, GetEnvelopeResultState.Success);
        }

        public static GetEnvelopeResult Error()
        {
            return new GetEnvelopeResult(null, GetEnvelopeResultState.Exception);
        }
    }
    public enum GetEnvelopeResultState
    {
        Success,
        Empty,
        Exception,
        Retry
    }


}