#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using NUnit.Framework;
using Sample;

namespace SaaS.Aggregates.Security
{
    public sealed class update_security_item_display_name : security_syntax
    {

        public static readonly SecurityId id = new SecurityId(42);
        public static readonly UserId user = new UserId(15);

        [Test]
        public void update_display()
        {
            Given(new SecurityAggregateCreated(id),
                        new SecurityIdentityAdded(id, new UserId(15), "my key", "legacy-key", "token"));
            When(new UpdateSecurityItemDisplayName(id, user, "new display"));
            Expect(new SecurityItemDisplayNameUpdated(id, user, "new display"));
        }

        [Test]
        public void update_dispay_duplicate()
        {
            Given(new SecurityAggregateCreated(id),
                        new SecurityIdentityAdded(id, user, "same display", "legacy-key", "generated-32"));
            When(new UpdateSecurityItemDisplayName(id, user, "same display"));
            Expect();
        }
    }
}