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
    public class report_login_success : user_syntax
    {
        static readonly UserId id = new UserId(1);
        static readonly SecurityId sec = new SecurityId(1);
        static readonly TimeSpan fiveMins = TimeSpan.FromMinutes(5);

        [Test]
        public void given_clear_history()
        {
            Given( new UserCreated(id, sec, fiveMins));
            When(new ReportUserLoginSuccess(id, Time(1, 07), "local"));
            Expect(new UserLoginSuccessReported(id, Time(1, 07), sec, "local"));
        }

        [Test]
        public void given_some_failures()
        {
            Given(new UserCreated(id, sec, fiveMins),
                        Failure(1),
                        Failure(2),
                        Failure(3),
                        Failure(4),
                        Success(4));
            When(new ReportUserLoginFailure(id, Time(1, 07), "local"));
            Expect(Failure(7));
        }



        static IEvent<UserId> Failure(int minutes)
        {
            return new UserLoginFailureReported(id, Time(1, minutes), sec, "local");
        }

        static IEvent<UserId> Success(int minutes)
        {
            return new UserLoginSuccessReported(id, Time(1, minutes), sec, "local");
        }
    }
}