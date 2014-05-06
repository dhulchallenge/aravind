#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Lokad.Cqrs.Envelope
{
    public abstract class AbstractMessageSerializer : IMessageSerializer
    {
        protected ICollection<Type> KnownTypes { get; private set; }
        readonly IDictionary<Type, Formatter> _formattersByType = new Dictionary<Type, Formatter>();
        readonly IDictionary<string, Formatter> _formattersByContract = new Dictionary<string, Formatter>();

        protected abstract Formatter PrepareFormatter(Type type);

        protected sealed class Formatter
        {
            public readonly string ContractName;
            public readonly Type FormatterType;
            public readonly Func<Stream, object> DeserializerDelegate;
            public readonly Action<object, Stream> SerializeDelegate;

            public Formatter(string contractName, Type formatterType, Func<Stream, object> deserializerDelegate, Action<object, Stream> serializeDelegate)
            {
                ContractName = contractName;
                DeserializerDelegate = deserializerDelegate;
                SerializeDelegate = serializeDelegate;
                FormatterType = formatterType;
            }
        }

        protected AbstractMessageSerializer(ICollection<Type> knownTypes)
        {
            KnownTypes = knownTypes;
            Build();
        }

        void Build()
        {
            foreach (var type in KnownTypes)
            {
                var formatter = PrepareFormatter(type);
                try
                {
                    _formattersByContract.Add(formatter.ContractName, formatter);
                }
                catch (ArgumentException ex)
                {
                    var msg = string.Format("Duplicate contract '{0}' being added to {1}", formatter.ContractName, GetType().Name);
                    throw new InvalidOperationException(msg, ex);
                }
                try
                {
                    _formattersByType.Add(type, formatter);
                }
                catch (ArgumentException e)
                {
                    var msg = string.Format("Duplicate type '{0}' being added to {1}", type, GetType().Name);
                    throw new InvalidOperationException(msg, e);
                }
            }
        }

        public void WriteMessage(object message, Type type, Stream stream)
        {
            Formatter formatter;
            if (!_formattersByType.TryGetValue(type, out formatter))
            {
                var s =
                    string.Format(
                        "Can't find serializer for unknown object type '{0}'. Have you passed all known types to the constructor?",
                        message.GetType());
                throw new InvalidOperationException(s);
            }
            using (var bin = new BitWriter(stream))
            {
                bin.Write(formatter.ContractName);
                byte[] buffer;
                using (var inner = new MemoryStream())
                {
                    // Some formatter implementations close the stream after writing.
                    // kudos to Slav for reminding that
                    formatter.SerializeDelegate(message, inner);
                    buffer = inner.ToArray();
                }
                bin.Write7BitInt(buffer.Length);
                bin.Write(buffer);
            }
        }

        sealed class BitReader : BinaryReader
        {
            public BitReader(Stream input) : base(input) { }

            public int Read7BitInt()
            {
                return Read7BitEncodedInt();
            }
            protected override void Dispose(bool disposing)
            {
                // don't do anything
            }
        }

        sealed class BitWriter : BinaryWriter
        {
            public BitWriter(Stream output) : base(output) { }
            public void Write7BitInt(int value)
            {
                Write7BitEncodedInt(value);
            }

            protected override void Dispose(bool disposing)
            {
                Flush();
            }
        }

        public object ReadMessage(Stream stream)
        {
            using (var bin = new BitReader(stream))
            {
                var contract = bin.ReadString();
                Formatter formatter;
                if (!_formattersByContract.TryGetValue(contract, out formatter))
                    throw new InvalidOperationException(string.Format("Couldn't find contract type for name '{0}'", contract));
                var length = bin.Read7BitInt();
                var data = bin.ReadBytes(length);
                using (var inner = new MemoryStream(data, 0,length))
                {
                    return formatter.DeserializerDelegate(inner);
                }
            }
        }

        static readonly MessageAttribute[] Empty = new MessageAttribute[0];
        public MessageAttribute[] ReadAttributes(Stream stream)
        {
            using (var reader = new BitReader(stream))
            {
                var attributeCount = reader.Read7BitInt();
                if (attributeCount == 0) return Empty;

                var attributes = new MessageAttribute[attributeCount];

                for (var i = 0; i < attributeCount; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    attributes[i] = new MessageAttribute(key, value);
                }
                return attributes;
            }
        }

        public void WriteAttributes(ICollection<MessageAttribute> attributes, Stream stream)
        {
            using (var writer = new BitWriter(stream))
            {
                writer.Write7BitInt(attributes.Count);
                foreach (var attribute in attributes)
                {
                    writer.Write(attribute.Key ?? "");
                    writer.Write(attribute.Value ?? "");
                }
            }
        }

        public int ReadCompactInt(Stream stream)
        {
            using (var binary = new BitReader(stream))
            {
                return binary.Read7BitInt();
            }
        }
    

        public void WriteCompactInt(int value, Stream stream)
        {
            using (var binary = new BitWriter(stream))
            {
                binary.Write7BitInt(value);
            }
        }
    }
}