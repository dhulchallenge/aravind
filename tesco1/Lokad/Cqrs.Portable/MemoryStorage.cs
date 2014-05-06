#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Concurrent;
using System.Linq;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.TapeStorage;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Lokad.Cqrs
{

    public static class MemoryStorage
    {
        public static MemoryStorageConfig CreateConfig()
        {
            return new MemoryStorageConfig();
        }

        /// <summary>
        /// Creates the simplified nuclear storage wrapper around Atomic storage.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="strategy">The atomic storage strategy.</param>
        /// <returns></returns>
        public static NuclearStorage CreateNuclear(this MemoryStorageConfig dictionary, IDocumentStrategy strategy)
        {
            var container = new MemoryDocumentStore(dictionary.Data, strategy);
            return new NuclearStorage(container);
        }
        

        public static MemoryQueueReader CreateInbox(this MemoryStorageConfig storageConfig,  params string[] queueNames)
        {
            var queues = queueNames
                .Select(n => storageConfig.Queues.GetOrAdd(n, s => new BlockingCollection<byte[]>()))
                .ToArray();

            return new MemoryQueueReader(queues, queueNames);
        }

        public static IQueueWriter CreateQueueWriter(this MemoryStorageConfig storageConfig, string queueName)
        {
            var collection = storageConfig.Queues.GetOrAdd(queueName, s => new BlockingCollection<byte[]>());
            return new MemoryQueueWriter(collection, queueName);
        }

        public static MessageSender CreateMessageSender(this MemoryStorageConfig storageConfig, IEnvelopeStreamer streamer, string queueName)
        {
            return new MessageSender(streamer, CreateQueueWriter(storageConfig, queueName));
        }
    }
}