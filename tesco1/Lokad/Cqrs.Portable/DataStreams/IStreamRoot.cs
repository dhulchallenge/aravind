#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.Collections.Generic;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Storage root (Azure Blob account or file drive)
    /// </summary>
    public interface IStreamRoot
    {
        /// <summary>
        /// Gets the container reference, identified by it's name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>new container referece</returns>
        IStreamContainer GetContainer(string name);


        IEnumerable<string> ListContainers(string prefix = null);
    }


}