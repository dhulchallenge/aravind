using System;
using System.Collections.Generic;
using Lokad.Cqrs;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class ImmutableEnvelopeTest
    {
        [Test]
        public void when_create_instance()
        {
            var date = DateTime.UtcNow;
            var immutableEnvelope = new ImmutableEnvelope("id", date, new List<string>(), new[] { new MessageAttribute("attr1", "val1"), });

            Assert.AreEqual("id", immutableEnvelope.EnvelopeId);
            Assert.AreEqual(date, immutableEnvelope.CreatedUtc);
            Assert.AreEqual(1, immutableEnvelope.Attributes.Count);
            Assert.IsTrue(immutableEnvelope.Message.GetType() == typeof(List<string>));
        }

        [Test]
        public void when_attributes_null()
        {
            var immutableEnvelope = new ImmutableEnvelope("id", DateTime.UtcNow, new List<string>(), null);
            
            CollectionAssert.AreEquivalent(MessageAttribute.Empty, immutableEnvelope.Attributes);
        }

        [Test]
        public void when_get_attribute()
        {
            var immutableEnvelope = new ImmutableEnvelope("id", DateTime.UtcNow, new object(), new[] { new MessageAttribute("attr1", "val1"), });
            var attributeVal = immutableEnvelope.GetAttribute("attr1");

            Assert.AreEqual("val1", attributeVal);
        }

        [Test]
        public void when_attribute_exist_and_get_value()
        {
            var immutableEnvelope = new ImmutableEnvelope("id", DateTime.UtcNow, new object(), new[] { new MessageAttribute("attr1", "val1"), });
            var attributeVal = immutableEnvelope.GetAttribute("attr1", "default");

            Assert.AreEqual("val1", attributeVal);
        }

        [Test]
        public void when_attribute_not_contains_and_get_default_value()
        {
            var immutableEnvelope = new ImmutableEnvelope("id", DateTime.UtcNow, new object(), new[] { new MessageAttribute("attr1", "val1"), });
            var attributeVal = immutableEnvelope.GetAttribute("attr2", "default");

            Assert.AreEqual("default", attributeVal);
        }
    }
}