#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System;
using System.Linq;
using System.Text;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Microsoft.WindowsAzure;
using NUnit.Framework;

namespace Cqrs.Azure.Tests
{
    public class TestEnvelopeStreamer : IEnvelopeStreamer
    {
        public ImmutableEnvelope Envelope { get; private set; }
        public byte[] Buffer { get; set; }

        public TestEnvelopeStreamer()
        { }

        public TestEnvelopeStreamer(byte[] buffer)
        {
            Buffer = buffer;
        }

        public byte[] SaveEnvelopeData(ImmutableEnvelope envelope)
        {
            Envelope = envelope;
            Buffer = new byte[] { 1, 2, 3 };

            return Buffer;
        }

        public ImmutableEnvelope ReadAsEnvelopeData(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException();
            return new ImmutableEnvelope("EnvId", DateTime.UtcNow, "Test meesage", new[] { new MessageAttribute("key", "value"), });
        }
    }
}