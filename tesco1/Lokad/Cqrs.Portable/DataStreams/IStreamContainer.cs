#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Represents storage container reference.
    /// </summary>
    public interface IStreamContainer
    {
        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <value>The full path.</value>
        string FullPath { get; }

        /// <summary>
        /// Gets the child container nested within the current container reference.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        IStreamContainer GetContainer(string name);

        Stream OpenRead(string name);
        Stream OpenWrite(string name);
        void TryDelete(string name);
        bool Exists(string name);


        /// <summary>
        /// Ensures that the current reference represents valid container
        /// </summary>
        /// <returns></returns>
        IStreamContainer Create();

        /// <summary>
        /// Deletes this container
        /// </summary>
        void Delete();

        /// <summary>
        /// Checks if the underlying container exists
        /// </summary>
        /// <returns></returns>
        bool Exists();

        IEnumerable<string> ListAllNestedItems();
        IEnumerable<StreamItemDetail> ListAllNestedItemsWithDetail();
    }
    public sealed class StreamItemDetail
    {
        public string Name;
        public DateTime LastModifiedUtc;
        public long Length;
    }
}