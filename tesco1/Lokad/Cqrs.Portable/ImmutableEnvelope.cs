#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Cqrs
{
    /// <summary>
    /// Deserialized message representation
    /// </summary>
    public class ImmutableEnvelope
    {
        public readonly string EnvelopeId;
        public readonly object Message;
        public DateTime CreatedUtc;
        public readonly ICollection<MessageAttribute> Attributes;

        public ImmutableEnvelope(string envelopeId, DateTime createdUtc, object message, MessageAttribute[] attributes)
        {
            
            EnvelopeId = envelopeId;
            Message = message;
            CreatedUtc = createdUtc;

            if (null == attributes)
            {
                Attributes = MessageAttribute.Empty;
            }
            else
            {
                // just ensuring that we have an immutable copy
                var copy = new MessageAttribute[attributes.Length];
                Array.Copy(attributes, copy, attributes.Length);
                Attributes = copy;
            }
        }

        public string GetAttribute(string name)
        {
            return Attributes.First(n => n.Key == name).Value;
        }

        public string GetAttribute(string name, string defaultValue)
        {
            foreach (var attribute in Attributes)
            {
                if (attribute.Key == name)
                    return attribute.Value;
            }
            return defaultValue;
        }
    }

    public struct MessageAttribute 
    {
        public readonly string Key;
        public readonly string Value;

        public static readonly MessageAttribute[] Empty = new MessageAttribute[0];

        public MessageAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}