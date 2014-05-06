using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Lokad.Cqrs
{
    public sealed class RedirectToCommand : HideObjectMembersFromIntelliSense
    {
        public readonly IDictionary<Type, Action<object>> Dict = new Dictionary<Type, Action<object>>();

        static readonly MethodInfo InternalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);


        public void WireToWhen(object o)
        {
            WireToMethod(o,"When");
        }

        public void WireToMethod(object o, string methodName)
        {
            var infos = o.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == methodName)
                .Where(m => m.GetParameters().Length == 1);

            foreach (var methodInfo in infos)
            {
                var type = methodInfo.GetParameters().First().ParameterType;

                var info = methodInfo;
                Dict.Add(type, message => info.Invoke(o, new[] { message }));
            }
        }

        public void WireToLambda<T>(Action<T> handler)
        {
            Dict.Add(typeof(T), o => handler((T)o));
        }

        public void InvokeMany(IEnumerable<object> messages, Action<object> onNull = null)
        {
            foreach (var message in messages)
            {
                Invoke(message, onNull);
            }
        }

        [DebuggerNonUserCode]
        public void Invoke(object message, Action<object> onNull = null)
        {
            Action<object> handler;
            var type = message.GetType();
            if (!Dict.TryGetValue(type, out handler))
            {
                handler = onNull ?? (o => { throw new InvalidOperationException("Failed to locate command handler for " + type); });
                //Trace.WriteLine(string.Format("Discarding {0} - failed to locate event handler", type.Name));

            }
            try
            {
                handler(message);
            }
            catch (TargetInvocationException ex)
            {
                if (null != InternalPreserveStackTraceMethod)
                    InternalPreserveStackTraceMethod.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
    }
    /// <summary>
    /// Creates convention-based routing rules
    /// </summary>
    public sealed class RedirectToDynamicEvent
    {
        public readonly IDictionary<Type, List<Wire>> Dict = new Dictionary<Type, List<Wire>>();


        public sealed class Wire
        {
            public Action<object> Call;
            public Type ParameterType;
        }

        static readonly MethodInfo InternalPreserveStackTraceMethod =
            typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);



        public void WireToWhen(object o)
        {
            var infos = o.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "When")
                .Where(m => m.GetParameters().Length == 1);

            foreach (var methodInfo in infos)
            {
                if (null == methodInfo)
                    throw new InvalidOperationException();

                var wires = new HashSet<Type>();
                var parameterType = methodInfo.GetParameters().First().ParameterType;
                wires.Add(parameterType);


                // if this is an interface, then we wire up to all inheritors in loaded assemblies
                // TODO: make this explicit
                if (parameterType.IsInterface)
                {
                    throw new InvalidOperationException("We don't support wiring to interfaces");
                    //var inheritors = typeof(StartProjectRun).Assembly.GetExportedTypes().Where(parameterType.IsAssignableFrom);
                    //foreach (var inheritor in inheritors)
                    //{
                    //    wires.Add(inheritor);
                    //}
                }

                foreach (var type in wires)
                {

                    List<Wire> list;
                    if (!Dict.TryGetValue(type, out list))
                    {
                        list = new List<Wire>();
                        Dict.Add(type, list);
                    }
                    var wire = BuildWire(o, type, methodInfo);
                    list.Add(wire);
                }
            }


        }

        static Wire BuildWire(object o, Type type, MethodInfo methodInfo)
        {
            var info = methodInfo;
            var dm = new DynamicMethod("MethodWrapper", null, new[] { typeof(object), typeof(object) });
            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, o.GetType());
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, type);
            il.EmitCall(OpCodes.Call, info, null);
            il.Emit(OpCodes.Ret);

            var call = (Action<object, object>)dm.CreateDelegate(typeof(Action<object, object>));
            var wire = new Wire
            {
                Call = o1 => call(o, o1),
                ParameterType = type
            };
            return wire;
        }

        public void WireTo<TMessage>(Action<TMessage> msg)
        {
            var type = typeof(TMessage);

            List<Wire> list;
            if (!Dict.TryGetValue(type, out list))
            {
                list = new List<Wire>();
                Dict.Add(type, list);
            }
            list.Add(new Wire
            {
                Call = o => msg((TMessage)o)
            });
        }



        [DebuggerNonUserCode]
        public void InvokeEvent(object @event)
        {
            var type = @event.GetType();
            List<Wire> info;
            if (!Dict.TryGetValue(type, out info))
            {
                return;
            }
            try
            {
                foreach (var wire in info)
                {
                    wire.Call(@event);
                }
            }
            catch (TargetInvocationException ex)
            {
                if (null != InternalPreserveStackTraceMethod)
                    InternalPreserveStackTraceMethod.Invoke(ex.InnerException, new object[0]);
                throw ex.InnerException;
            }
        }
    }

}