#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using ProtoBuf;

namespace Cqrs.Portable.Tests
{
    public interface ISampleMessage { }

    public interface ICommand : ISampleMessage { }
    public interface IEvent : ISampleMessage { }
    public interface IFuncCommand : ICommand { }

    public interface IFuncEvent : IEvent { }
    /// <summary>
    /// Semi strongly-typed message sending endpoint made
    ///  available to stateless workflow processes.
    /// </summary>
    public interface ICommandSender
    {
        /// <summary>
        /// This interface is intentionally made long and unusable. Generally within the domain 
        /// (as in Mousquetaires domain) there will be extension methods that provide strongly-typed
        /// extensions (that don't allow sending wrong command to wrong location).
        /// </summary>
        /// <param name="commands">The commands.</param>
        void SendCommand(ICommand commands, bool idFromContent = false);
    }

    public sealed class TypedMessageSender : ICommandSender
    {
        readonly MessageSender _commandRouter;
        readonly MessageSender _functionalRecorder;


        public TypedMessageSender(MessageSender commandRouter, MessageSender functionalRecorder)
        {
            _commandRouter = commandRouter;
            _functionalRecorder = functionalRecorder;
        }

        public void SendCommand(ICommand commands, bool idFromContent = false)
        {
            if (idFromContent)
            {
                _commandRouter.SendHashed(commands);
            }
            else
            {
                _commandRouter.Send(commands);
            }
        }

        public void SendFromClient(ICommand e, string id, MessageAttribute[] attributes)
        {
            _commandRouter.Send(e, id, attributes);
        }

        public void PublishFromClientHashed(IFuncEvent e, MessageAttribute[] attributes)
        {
            _functionalRecorder.SendHashed(e, attributes);
        }

        public void Publish(IFuncEvent @event)
        {
            _functionalRecorder.Send(@event);
        }

        public void Send(IFuncCommand command)
        {
            _commandRouter.Send(command);
        }

        public void SendHashed(IFuncCommand command)
        {
            _commandRouter.SendHashed(command);
        }


    }

    static class NameCache<T>
    {
        // ReSharper disable StaticFieldInGenericType
        public static readonly string Name;
        public static readonly string Namespace;
        // ReSharper restore StaticFieldInGenericType
        static NameCache()
        {
            var type = typeof(T);

            Name = new string(Splice(type.Name).ToArray()).TrimStart('-');
            var dcs = type.GetCustomAttributes(false).OfType<DataContractAttribute>().ToArray();


            if (dcs.Length <= 0) return;
            var attribute = dcs.First();

            if (!string.IsNullOrEmpty(attribute.Name))
            {
                Name = attribute.Name;
            }

            if (!string.IsNullOrEmpty(attribute.Namespace))
            {
                Namespace = attribute.Namespace;
            }
        }

        static IEnumerable<char> Splice(string source)
        {
            foreach (var c in source)
            {
                if (char.IsUpper(c))
                {
                    yield return '-';
                }
                yield return char.ToLower(c);
            }
        }
    }

    public sealed class DocumentStrategy : IDocumentStrategy
    {
        public void Serialize<TEntity>(TEntity entity, Stream stream)
        {
            // ProtoBuf must have non-zero files
            stream.WriteByte(42);
            Serializer.Serialize(stream, entity);
        }

        public TEntity Deserialize<TEntity>(Stream stream)
        {
            var signature = stream.ReadByte();

            if (signature != 42)
                throw new InvalidOperationException("Unknown view format");

            return Serializer.Deserialize<TEntity>(stream);
        }

        public string GetEntityBucket<TEntity>()
        {
            return "sample-doc" + "/" + NameCache<TEntity>.Name;
        }

        public string GetEntityLocation<TEntity>(object key)
        {
            if (key is unit)
                return NameCache<TEntity>.Name + ".pb";

            return key.ToString().ToLowerInvariant() + ".pb";
        }
    }
}