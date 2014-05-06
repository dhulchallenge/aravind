#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License
// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence
#endregion

using System.Text;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Feature.AzurePartition;
using NUnit.Framework;

namespace Cqrs.Azure.Tests.Partition
{
    public class AzureMessageOverflowsTest
    {
        [Test]
        public void when_save_envelope_reference()
        {
            var result = Encoding.Unicode.GetString(AzureMessageOverflows.SaveEnvelopeReference(new EnvelopeReference("container", "reference")));

            Assert.AreEqual("[cqrs-ref-r1]\r\ncontainer\r\nreference", result);
        }

        [Test]
        public void when_read_bad_envelope_reference()
        {
            EnvelopeReference envelopeReference;
            var result = AzureMessageOverflows.TryReadAsEnvelopeReference(new byte[0], out envelopeReference);

            Assert.IsFalse(result);
            Assert.IsNull(envelopeReference);
        }

        [Test]
        public void when_read_envelope_reference()
        {
            EnvelopeReference envelopeReference;
            var result = AzureMessageOverflows.TryReadAsEnvelopeReference(Encoding.Unicode.GetBytes("[cqrs-ref-r1]\r\ncontainer\r\nreference"), out envelopeReference);

            Assert.IsTrue(result);
            Assert.AreEqual("container", envelopeReference.StorageContainer);
            Assert.AreEqual("reference", envelopeReference.StorageReference);
        }
    }
}