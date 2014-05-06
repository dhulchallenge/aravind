#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lokad.Cqrs.StreamingStorage;
using Microsoft.WindowsAzure.StorageClient;
using System.Linq;

namespace Lokad.Cqrs.Feature.StreamingStorage
{
    /// <summary>
    /// Windows Azure implementation of storage 
    /// </summary>
    public sealed class BlobStreamingContainer : IStreamContainer
    {
        readonly CloudBlobDirectory _directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamingContainer"/> class.
        /// </summary>
        /// <param name="directory">The directory.</param>
        public BlobStreamingContainer(CloudBlobDirectory directory)
        {
            _directory = directory;
        }

        public IStreamContainer GetContainer(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            return new BlobStreamingContainer(_directory.GetSubdirectory(name));
        }

        public Stream OpenRead(string name)
        {
            return _directory.GetBlobReference(name).OpenRead();
        }

        public Stream OpenWrite(string name)
        {
            return _directory.GetBlobReference(name).OpenWrite();
        }

        public void TryDelete(string name)
        {
            _directory.GetBlobReference(name).DeleteIfExists();
        }

        public bool Exists(string name)
        {
            try
            {
                _directory.GetBlobReference(name).FetchAttributes();
                return true;
            }
            catch (StorageClientException ex)
            {
                return false;
            }
        }

        
        public IStreamContainer Create()
        {
            _directory.Container.CreateIfNotExist();
            return this;
        }

        /// <summary>
        /// Deletes this container
        /// </summary>
        public void Delete()
        {
            try
            {
                if (_directory.Uri.ToString().Trim('/') == _directory.Container.Uri.ToString().Trim('/'))
                {
                    _directory.Container.Delete();
                }
                else
                {
                    _directory.ListBlobs().AsParallel().ForAll(l =>
                        {
                            var name = l.Parent.Uri.MakeRelativeUri(l.Uri).ToString();
                            var r = _directory.GetBlobReference(name);
                            r.BeginDeleteIfExists(ar => { }, null);
                        });
                }
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        return;
                    default:
                        throw;
                }
            }
        }

        public IEnumerable<string> ListAllNestedItems()
        {
            try
            {
                return _directory.ListBlobs()
                    .Select(item => _directory.Uri.MakeRelativeUri(item.Uri).ToString())
                    .ToArray();
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                            this.FullPath);
                        throw new StreamContainerNotFoundException(message, e);
                    default:
                        throw;
                }
            }
        }

        public IEnumerable<StreamItemDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _directory.ListBlobs(new BlobRequestOptions())
                    .OfType<CloudBlob>()
                    .Select(item => new StreamItemDetail()
                        {
                            Name = _directory.Uri.MakeRelativeUri(item.Uri).ToString(),
                            LastModifiedUtc = item.Properties.LastModifiedUtc,
                            Length = item.Properties.Length
                        })
                    .ToArray();
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                        var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                            this.FullPath);
                        throw new StreamContainerNotFoundException(message, e);
                    default:
                        throw;
                }
            }
        }

        public bool Exists()
        {
            try
            {
                _directory.Container.FetchAttributes();
                return true;
            }
            catch (StorageClientException e)
            {
                switch (e.ErrorCode)
                {
                    case StorageErrorCode.ContainerNotFound:
                    case StorageErrorCode.ResourceNotFound:
                    case StorageErrorCode.BlobNotFound:
                        return false;
                    default:
                        throw;
                }
            }
        }

        public string FullPath
        {
            get { return _directory.Uri.ToString(); }
        }
    }
}