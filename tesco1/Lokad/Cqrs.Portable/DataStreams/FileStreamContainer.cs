#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Storage container using <see cref="System.IO"/> for persisting data
    /// </summary>
    public sealed class FileStreamContainer : IStreamContainer, IStreamRoot
    {
        readonly DirectoryInfo _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStreamContainer"/> class.
        /// </summary>
        /// <param name="root">The root.</param>
        public FileStreamContainer(DirectoryInfo root)
        {
            _root = root;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStreamContainer"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public FileStreamContainer(string path) : this(new DirectoryInfo(path))
        {
        }

        public IStreamContainer GetContainer(string name)
        {
            var child = new DirectoryInfo(Path.Combine(_root.FullName, name));
            return new FileStreamContainer(child);
        }

        public IEnumerable<string> ListContainers(string prefix = null)
        {
            if (string.IsNullOrEmpty(prefix))
                return _root.GetDirectories().Select(d => d.Name);
            return _root.GetDirectories(prefix + "*").Select(d => d.Name);
        }

        public Stream OpenRead(string name)
        {
            var combine = Path.Combine(_root.FullName, name);

            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine,FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite(string name)
        {
            var combine = Path.Combine(_root.FullName, name);
            
            // we allow concurrent reading
            // no more writers are allowed
            return File.Open(combine,FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        }

        public void TryDelete(string name)
        {
            var combine = Path.Combine(_root.FullName, name);
            File.Delete(combine);
        }

        public bool Exists(string name)
        {
            return File.Exists(Path.Combine(_root.FullName, name));
        }

        public IStreamContainer Create()
        {
            _root.Create();
            return this;
        }

        public void Delete()
        {
            _root.Refresh();
            if (_root.Exists)
                _root.Delete(true);
        }

        public bool Exists()
        {
            _root.Refresh();
            return _root.Exists;
        }

        public IEnumerable<string> ListAllNestedItems()
        {
            try
            {
                return _root.GetFiles().Select(f => f.Name).ToArray();
            }
            catch (DirectoryNotFoundException e)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                    this.FullPath);
                throw new StreamContainerNotFoundException(message, e);
            }
        }

        public IEnumerable<StreamItemDetail> ListAllNestedItemsWithDetail()
        {
            try
            {
                return _root.GetFiles("*", SearchOption.AllDirectories).Select(f => new StreamItemDetail()
                    {
                        LastModifiedUtc = f.LastWriteTimeUtc,
                        Length = f.Length,
                        Name = f.Name
                    }).ToArray();
            }
            catch (DirectoryNotFoundException e)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "Storage container was not found: '{0}'.",
                    this.FullPath);
                throw new StreamContainerNotFoundException(message, e);
            }
        }

        public string FullPath
        {
            get { return _root.FullName; }
        }
    }
}