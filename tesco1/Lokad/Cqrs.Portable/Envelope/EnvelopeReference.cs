﻿#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

namespace Lokad.Cqrs.Envelope
{
    public sealed class EnvelopeReference
    {
        public readonly string StorageReference;
        public readonly string StorageContainer;

        public EnvelopeReference(string storageContainer, string storageReference)
        {
            StorageReference = storageReference;
            StorageContainer = storageContainer;
        }
    }
}