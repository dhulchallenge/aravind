#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Cqrs.Envelope
{
    ///<summary>
    /// Shoud be registered as singleton, manages actual memories
    /// and performs cleanups in arun
    ///</summary>
    public sealed class DuplicationManager : IEngineProcess
    {
        readonly ConcurrentDictionary<object, DuplicationMemory> _memories =
            new ConcurrentDictionary<object, DuplicationMemory>();

        public void Dispose()
        {
        }

        public void Initialize(CancellationToken token)
        {
        }

        public DuplicationMemory GetOrAdd(object dispatcher)
        {
            return _memories.GetOrAdd(dispatcher, s => new DuplicationMemory());
        }

        public Task Start(CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        foreach (var memory in _memories)
                        {
                            memory.Value.ForgetOlderThan(TimeSpan.FromMinutes(20));
                        }

                        token.WaitHandle.WaitOne(TimeSpan.FromMinutes(5));
                    }
                }, token);
        }
    }
}