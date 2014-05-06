#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using NUnit.Framework;
using Sample;

namespace SaaS.Aggregates.Security
{
    public class remove_security_item : security_syntax
    {
        public static readonly SecurityId id = new SecurityId(42);
        public static readonly UserId user = new UserId(15);

        [Test]
        public void given_password()
        {
            Given(new SecurityAggregateCreated(id),
                        new SecurityPasswordAdded(id, new UserId(15), "my pass", "user", "hash", "salt", "token"));
            When(new RemoveSecurityItem(id, user));
            Expect(new SecurityItemRemoved(id, user, "user", "password"));
        }

        

        [Test]
        public void given_identity()
        {
            Given(new SecurityAggregateCreated(id),
                        new SecurityIdentityAdded(id, new UserId(15), "my ID", "openId", "token"));
            When(new RemoveSecurityItem(id, user));
            Expect(new SecurityItemRemoved(id, user, "openId", "identity"));
        }
    }
}