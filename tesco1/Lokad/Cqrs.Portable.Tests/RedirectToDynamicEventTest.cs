using System;
using Lokad.Cqrs;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class RedirectToDynamicEventTest
    {
        [Test]
        public void when_wire_to_object()
        {
            var dynamicEvent = new RedirectToDynamicEvent();
            var testClass = new TestClassWithMethod();
            dynamicEvent.WireToWhen(testClass);
            var t = new TestRedirectMethod { Id = 333, Name = "Name" };
            dynamicEvent.InvokeEvent(t);

            Assert.AreEqual(t, testClass.RedirectMethod);
        }

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void when_wire_to_object_where_argument_is_interface()
        {
            var dynamicEvent = new RedirectToDynamicEvent();
            var testClass = new TestClassWithInterfaceMethod();
            dynamicEvent.WireToWhen(testClass);
        }

        [Test]
        public void when_wire_to_action()
        {
            var dynamicEvent = new RedirectToDynamicEvent();
            var testClass = new TestClassWithMethod();
            dynamicEvent.WireTo<int>(i => { testClass.Summa += i; });
            dynamicEvent.InvokeEvent(4);
            dynamicEvent.InvokeEvent(4);

            Assert.AreEqual(8, testClass.Summa);
        }
    }
}