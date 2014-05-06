
using System.Collections.Generic;
using Lokad.Cqrs;
using NUnit.Framework;

namespace Cqrs.Portable.Tests
{
    public class RedirectToCommandTest
    {
        [Test]
        public void when_wire_to_method()
        {
            var command = new RedirectToCommand();
            var testClass = new TestClassWithMethod();
            command.WireToMethod(testClass, "Method1");
            command.Invoke("value1");

            Assert.IsTrue(testClass.CallMethod1);
        }

        [Test]
        public void when_wire_to_when_method()
        {
            var command = new RedirectToCommand();
            var testClass = new TestClassWithMethod();
            command.WireToWhen(testClass);
            var t = new TestRedirectMethod { Id = 333, Name = "Name" };
            command.Invoke(t);

            Assert.AreEqual(t, testClass.RedirectMethod);
        }

        [Test]
        public void when_wire_to_lamda()
        {
            var command = new RedirectToCommand();
            var testClass = new TestClassWithMethod();
            command.WireToLambda<int>(i => { testClass.Summa += i; });
            command.Invoke(4);
            command.Invoke(4);

            Assert.AreEqual(8, testClass.Summa);
        }

        [Test]
        public void when_many_invoke()
        {
            var command = new RedirectToCommand();
            var testClass = new TestClassWithMethod();
            command.WireToMethod(testClass, "Method2");
            command.InvokeMany(new List<string> { "value1", "value2", "value3" });

            Assert.AreEqual(3, testClass.List.Count);
            Assert.AreEqual("value1", testClass.List[0]);
            Assert.AreEqual("value2", testClass.List[1]);
            Assert.AreEqual("value3", testClass.List[2]);
        }
    }

    public class TestClassWithMethod
    {
        public int Summa { get; set; }
        public bool CallMethod1 { get; private set; }
        public string ParamValue { get; private set; }
        public TestRedirectMethod RedirectMethod { get; set; }
        public List<string> List { get; private set; }

        public TestClassWithMethod()
            : this(0)
        { }

        public TestClassWithMethod(int val0)
        {
            List = new List<string>();
            Summa += val0;
            CallMethod1 = false;
        }

        public void Method1(string param1)
        {
            ParamValue = param1;
            CallMethod1 = true;
        }

        public void When(TestRedirectMethod redirectMethod)
        {
            RedirectMethod = redirectMethod;
        }

        public void When(int val)
        {
            Summa += val;
        }

        public void Method2(string param1)
        {
            List.Add(param1);
        }

        
    }

    public class TestClassWithInterfaceMethod
    {
        public void When(TestInterface arg)
        {

        }
    }

    public class TestRedirectMethod : TestInterface
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface TestInterface
    {
        int Id { get; set; }
        string Name { get; set; }
    }
}