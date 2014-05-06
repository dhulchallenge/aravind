#region (c) 2010-2011 Lokad CQRS - New BSD License 

// Copyright (c) Lokad SAS 2010-2011 (http://www.lokad.com)
// This code is released as Open Source under the terms of the New BSD Licence
// Homepage: http://lokad.github.com/lokad-cqrs/

#endregion

using System;
using System.Threading;
using Lokad.Cqrs.AppendOnly;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.Feature.AzurePartition;
using Lokad.Cqrs.Feature.AzurePartition.Inbox;
using Lokad.Cqrs.Feature.StreamingStorage;
using Lokad.Cqrs.StreamingStorage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Lokad.Cqrs
{
    /// <summary>
    /// Helper class to access Azure storage outside of the engine, if needed
    /// </summary>
    public static class AzureStorage
    {
        /// <summary> Creates the simplified nuclear storage wrapper around Atomic storage. </summary>
        /// <param name="config">The storage config.</param>
        /// <param name="strategy">The atomic storage strategy.</param>
        /// <returns></returns>
        public static IDocumentStore CreateDocumentStore(this IAzureStorageConfig config,
            IDocumentStrategy strategy)
        {
            var client = config.CreateBlobClient();
            return new AzureDocumentStore(strategy, client);
        }


        /// <summary> Creates the storage access configuration. </summary>
        /// <param name="cloudStorageAccount">The cloud storage account.</param>
        /// <param name="storageConfigurationStorage">The config storage.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(CloudStorageAccount cloudStorageAccount,
            Action<AzureStorageConfigurationBuilder> storageConfigurationStorage)
        {
            var builder = new AzureStorageConfigurationBuilder(cloudStorageAccount);
            storageConfigurationStorage(builder);

            return builder.Build();
        }

        /// <summary>
        /// Creates the storage access configuration.
        /// </summary>
        /// <param name="storageString">The storage string.</param>
        /// <param name="storageConfiguration">The storage configuration.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(string storageString,
            Action<AzureStorageConfigurationBuilder> storageConfiguration)
        {
            return CreateConfig(CloudStorageAccount.Parse(storageString), storageConfiguration);
        }

        /// Creates the storage access configuration.
        /// <param name="storageString">The storage string.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(string storageString)
        {
            return CreateConfig(storageString, builder => { });
        }

        /// <summary>
        /// Creates the storage access configuration for the development storage emulator.
        /// </summary>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfigurationForDev()
        {
            return CreateConfig(CloudStorageAccount.DevelopmentStorageAccount, c => c.Named("azure-dev"));
        }

        /// <summary>
        /// Creates the storage access configuration.
        /// </summary>
        /// <param name="cloudStorageAccount">The cloud storage account.</param>
        /// <returns></returns>
        public static IAzureStorageConfig CreateConfig(CloudStorageAccount cloudStorageAccount)
        {
            return CreateConfig(cloudStorageAccount, builder => { });
        }

        /// <summary>
        /// Creates the streaming storage out of the provided storage config.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        public static IStreamRoot CreateStreaming(this IAzureStorageConfig config)
        {
            return new BlobStreamingRoot(config.CreateBlobClient());
        }


        public static IStreamContainer CreateStreaming(this IAzureStorageConfig config, string container)
        {
            return config.CreateStreaming().GetContainer(container).Create();
        }

       

        public static MessageSender CreateMessageSender(this IAzureStorageConfig account,
            IEnvelopeStreamer streamer, string queueName)
        {
            return new MessageSender(streamer, CreateQueueWriter(account, queueName));
        }

        public static StatelessAzureQueueReader BuildIntake(IAzureStorageConfig cfg, string name,
            TimeSpan visibilityTimeout = default(TimeSpan))
        {
            var timeout = visibilityTimeout == default(TimeSpan) ? TimeSpan.FromMinutes(5) : visibilityTimeout;

            var queue = cfg.CreateQueueClient().GetQueueReference(name);

            var container = cfg.CreateBlobClient().GetBlobDirectoryReference("queues-big").GetSubdirectory(name);

            var poisonQueue = new Lazy<CloudQueue>(() =>
                {
                    var queueReference = cfg.CreateQueueClient().GetQueueReference(name + "-poison");
                    queueReference.CreateIfNotExist();
                    return queueReference;
                }, LazyThreadSafetyMode.ExecutionAndPublication);

            var reader = new StatelessAzureQueueReader(name, queue, container, poisonQueue, timeout);
            reader.InitIfNeeded();
            return reader;
        }

        public static AzureQueueReader CreateQueueReader(this IAzureStorageConfig cfg, string name,
            Func<uint, TimeSpan> decay = null, TimeSpan visibilityTimeout = default(TimeSpan))
        {
            var intake = BuildIntake(cfg, name, visibilityTimeout);
            var waiter = decay ?? DecayEvil.BuildExponentialDecay(2000);
            return new AzureQueueReader(new[] {intake}, waiter);
        }

        

        public static StatelessAzureQueueWriter CreateQueueWriter(this IAzureStorageConfig cfg, string queueName)
        {
            return StatelessAzureQueueWriter.Create(cfg, queueName);
        }
        public static BlobAppendOnlyStore CreateAppendOnlyStore(this IAzureStorageConfig config, string s)
        {
            var client = config.CreateBlobClient();
            var store = new BlobAppendOnlyStore(client.GetContainerReference(s));
            store.InitializeWriter();
            return store;
        }
    }
}