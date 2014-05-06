using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using SaaS.Aggregates.User;
using Sample;
using ServiceStack.Text;

namespace SaaS
{
    /// <summary>
    /// This class scans all available specifications for messages used
    /// then performs round-trip via specified serializer,
    /// and then does the structural comparison of resulting values
    /// </summary>
    [TestFixture]
    public sealed class TestMessageSerialization
    {
        static Group[] ListMessages()
        {
            var types =
                new[] { typeof(user_syntax) }
                    .SelectMany(t => t.Assembly.GetExportedTypes());

            return types
                .Where(t => typeof(IListSpecifications).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                .SelectMany(t => ((IListSpecifications)Activator.CreateInstance(t)).GetAll())
                .SelectMany(GetGroups)
                .Where(g => !(g.Message is IAmUsedByUnitTests))
                .GroupBy(g => g.Message.GetType())
                .Select(g => new Group(g.ToArray(), g.Key))
                .ToArray();
        }

        [Test, Explicit]
        public void WeHaveMessages()
        {
            CollectionAssert.IsNotEmpty(ListMessages());
        }

        public sealed class Group
        {
            public readonly IEnumerable<Source> Messages;
            public readonly Type Type;

            public Group(IEnumerable<Source> messages, Type type)
            {
                Messages = messages;
                Type = type;
            }

            public override string ToString()
            {
                return Type.Name + " x" + Messages.Count();
            }
        }

        public sealed class Source
        {
            public readonly ISampleMessage Message;
            public readonly string Origin;

            public Source(ISampleMessage message, string origin)
            {
                Message = message;
                Origin = origin;
            }
        }

        static IEnumerable<Source> GetGroups(Specification run)
        {
            var name = string.Format("{0} {1}", run.GroupName, run.CaseName);

            foreach (var @event in run.Given)
            {
                yield return new Source(@event, name + " Given");
            }
            yield return new Source(run.When, name + " When");
            foreach (var w in run.Then)
            {
                yield return new Source(w, name + " Expect");
            }
        }

        [TestCaseSource("ListMessages")]
        public void Verify(Group msgs)
        {
            var list = new List<string>();
            int count = 0;
            foreach (var exp in msgs.Messages)
            {
                count++;
                var expected = exp.Message;
                {
                    var type = expected.GetType();
                    var s = JsonSerializer.SerializeToString(expected, type);
                    var actual = JsonSerializer.DeserializeFromString(s, type);
                    var compare = CompareObjects.FindDifferences(expected, actual);
                    if (!string.IsNullOrEmpty(compare))
                    {
                        list.Add("JsonSerializer " + exp.Origin + Environment.NewLine + compare);
                    }
                }
                {
                    var actual = Serializer.DeepClone(expected);
                    var compare = CompareObjects.FindDifferences(expected, actual);
                    if (!string.IsNullOrWhiteSpace(compare))
                    {
                        list.Add("ProtoBuf " + exp.Origin + Environment.NewLine + compare);
                    }
                }
            }
            if (list.Count > 0)
            {
                Assert.Fail("{0} out of {1}\r\n{2}", list.Count, count, string.Join(Environment.NewLine, list));
            }
        }
    }
}