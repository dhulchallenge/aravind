#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using NUnit.Framework;
using Sample;

// ReSharper disable InconsistentNaming

namespace SaaS.Aggregates.User
{
    public class delete_user : user_syntax
    {
        static readonly UserId id = new UserId(1);
        static readonly SecurityId sec = new SecurityId(1);
        static readonly TimeSpan fiveMins = TimeSpan.FromMinutes(5);


        [Test]
        public void given_locked_user()
        {
            Given(new UserCreated(id, sec, fiveMins),
                        new UserLocked(id, "locked", sec, Time(1, 10))
                  );
            When(new DeleteUser(id));
            Expect(new UserDeleted(id, sec));
        }
    }
}