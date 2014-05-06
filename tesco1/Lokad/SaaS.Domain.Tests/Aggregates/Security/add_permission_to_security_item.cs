#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using NUnit.Framework;


namespace SaaS.Aggregates.Security
{
    public class add_permission_to_security_item : security_syntax
    {
        public static readonly SecurityId id = new SecurityId(42);
        public static readonly UserId user = new UserId(15);

        [Test]
        public void when_duplicate_permission()
        {
            Given(
                new SecurityAggregateCreated(id),
                new SecurityIdentityAdded(id, new UserId(15), "my key", "legacy-key", "generated-32"),
                new PermissionAddedToSecurityItem(id, user, "my key", "root", "generated-32"));

            When(new AddPermissionToSecurityItem(id, user, "root"));

            Expect();
        }

        [Test]
        public void when_valid_item()
        {
            Given(
                new SecurityAggregateCreated(id),
                new SecurityIdentityAdded(id, new UserId(15), "my key", "legacy-key", "generated-32"));

            When(new AddPermissionToSecurityItem(id, user, "root"));
            Expect(new PermissionAddedToSecurityItem(id, user, "my key", "root", "generated-32"));
        }

        [Test]
        public void non_existent_item()
        {
            Given(new SecurityAggregateCreated(id));
            When(new AddPermissionToSecurityItem(id, user, "root"));
            Expect(new ExceptionThrown("invalid-user"));
        }
    }
}