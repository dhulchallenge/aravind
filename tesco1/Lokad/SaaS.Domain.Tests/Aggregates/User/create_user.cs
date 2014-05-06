#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using NUnit.Framework;
using Sample;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace SaaS.Aggregates.User
{
    public class create_user : user_syntax
    {
        static UserId id = new UserId(1);
        static SecurityId sec = new SecurityId(1);

        [Test]
        public void given_no_prior_history()
        {
            Given();
            When(new CreateUser(id, sec));
            Expect(new UserCreated(id, sec, TimeSpan.FromMinutes(10)));
        }

        [Test]
        public void given_created_user()
        {
            Given(new UserCreated(id, sec, TimeSpan.FromMinutes(5)));
            When(new CreateUser(id, sec));
            Expect("rebirth");
        }
    }
}