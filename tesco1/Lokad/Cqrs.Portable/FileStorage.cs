#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.IO;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Lokad.Cqrs
{
    public static class FileStorage
    {
        public static IDocumentStore CreateDocumentStore(this FileStorageConfig config, IDocumentStrategy strategy)
        {
            return new FileDocumentStore(config.FullPath, strategy);
        }


        public static IStreamRoot CreateStreaming(this FileStorageConfig config)
        {
            var path = config.FullPath;
            var container = new FileStreamContainer(path);
            container.Create();
            return container;
        }
        public static IStreamContainer CreateStreaming(this FileStorageConfig config, string subfolder)
        {
            return config.CreateStreaming().GetContainer(subfolder).Create();
        }

        public static FileStorageConfig CreateConfig(string fullPath, string optionalName = null, bool reset = false)
        {
            var folder = new DirectoryInfo(fullPath);
            var config = new FileStorageConfig(folder, optionalName ?? folder.Name);
            if (reset)
            {
                config.Reset();
            }
            return config;
        }

        public static FileStorageConfig CreateConfig(DirectoryInfo info, string optionalName = null)
        {
            return new FileStorageConfig(info, optionalName ?? info.Name);
        }

        public static FileQueueReader CreateInbox(this FileStorageConfig cfg, string name, Func<uint, TimeSpan> decay = null)
        {
            var reader = new StatelessFileQueueReader(Path.Combine(cfg.FullPath, name), name);

            var waiter = decay ?? DecayEvil.BuildExponentialDecay(250);
            var inbox = new FileQueueReader(new[]{reader, }, waiter);
            inbox.Init();
            return inbox;
        }
        public static FileQueueWriter CreateQueueWriter(this FileStorageConfig cfg, string queueName)
        {
            var full = Path.Combine(cfg.Folder.FullName, queueName);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
            }
            return
                new FileQueueWriter(new DirectoryInfo(full), queueName);
        }
        public static FileAppendOnlyStore CreateAppendOnlyStore(this FileStorageConfig cfg, string name)
        {

            var store = new FileAppendOnlyStore(new DirectoryInfo(Path.Combine(cfg.FullPath, name)));
            store.Initialize();
            return store;
        }

        public static MessageSender CreateMessageSender(this FileStorageConfig account, IEnvelopeStreamer streamer, string queueName)
        {
            return new MessageSender(streamer, CreateQueueWriter(account, queueName));
        }
    }
}