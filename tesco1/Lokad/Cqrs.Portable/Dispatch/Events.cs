using System;
using Lokad.Cqrs.Partition;

namespace Lokad.Cqrs.Dispatch.Events
{
    [Serializable]
    public sealed class MessageAcked : ISystemEvent
    {
        public MessageTransportContext Context { get; private set; }

        public MessageAcked(MessageTransportContext attributes)
        {
            Context = attributes;
        }

        public override string ToString()
        {
            return string.Format("[{0}] acked at '{1}'", Context.TransportMessage, Context.QueueName);
        }
    }
    [Serializable]
    public sealed class MessageInboxFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public string InboxName { get; private set; }
        public string MessageId { get; private set; }
        public MessageInboxFailed(Exception exception, string inboxName, string messageId)
        {
            Exception = exception;
            InboxName = inboxName;
            MessageId = messageId;
        }

        public override string ToString()
        {
            return string.Format("Failed to retrieve message from {0}: {1}.", InboxName, Exception.Message);
        }
    }
    [Serializable]
    public sealed class DispatchRecoveryFailed : ISystemEvent
    {
        public Exception DispatchException { get; private set; }
        public MessageTransportContext Message { get; private set; }
        public string QueueName { get; private set; }

        public DispatchRecoveryFailed(Exception exception, MessageTransportContext message, string queueName)
        {
            DispatchException = exception;
            Message = message;
            QueueName = queueName;
        }

        public override string ToString()
        {
            return string.Format("Failed to recover dispatch '{0}' from '{1}': {2}", Message.TransportMessage, QueueName, DispatchException.Message);
        }
    }
    [Serializable]
    public sealed class MessageAckFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public MessageTransportContext Context { get; private set; }

        public MessageAckFailed(Exception exception, MessageTransportContext context)
        {
            Exception = exception;
            Context = context;
        }

        public override string ToString()
        {
            return string.Format("Failed to ack '{0}' from '{1}': {2}", Context.TransportMessage, Context.QueueName, Exception.Message);
        }
    }
    [Serializable]
    public sealed class MessageDispatchFailed : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public MessageTransportContext Message { get; private set; }
        public string QueueName { get; private set; }

        public MessageDispatchFailed(MessageTransportContext message, string queueName, Exception exception)
        {
            Exception = exception;
            Message = message;
            QueueName = queueName;
        }

        public override string ToString()
        {
            return string.Format("Failed to consume {0} from '{1}': {2}", Message.TransportMessage, QueueName,
                Exception.Message);
        }
    }


}