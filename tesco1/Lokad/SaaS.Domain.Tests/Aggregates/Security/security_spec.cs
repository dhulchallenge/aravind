#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using NUnit.Framework;
using Sample;

namespace SaaS.Aggregates.Security
{
    public abstract class security_syntax : spec_syntax<SecurityId>
    {
        public TestIdentityService<SecurityId> Identity;
        public PasswordGenerator Password = new TestPassword();
        public IMailSender Sender = new TestSendEmail();

        

        protected override void SetupServices()
        {
            Identity = new TestIdentityService<SecurityId>();
        }

        protected override void ExecuteCommand(IEventStore store, ICommand<SecurityId> cmd)
        {
            new SecurityApplicationService(store, Identity, Password, new TestUserIndexService<SecurityId>()).Execute(cmd);
        }
    }
}