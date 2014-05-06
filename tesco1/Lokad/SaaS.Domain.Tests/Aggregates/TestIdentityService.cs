#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using Sample;

namespace SaaS.Aggregates
{
    public sealed class TestIdentityService<T> : IDomainIdentityService where T : IIdentity
    {
        public IEvent<T> SetNextId(int id)
        {
            return new SpecSetupEvent<T>(() => _nextId = id, "Identity starts with " + id);
        }
        int _nextId;

        long IDomainIdentityService.GetId()
        {
            return _nextId++;
        }
    }
}