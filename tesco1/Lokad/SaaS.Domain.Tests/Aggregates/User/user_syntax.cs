#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

// ReSharper disable InconsistentNaming

using Sample;

namespace SaaS.Aggregates.User
{
    public abstract class user_syntax : spec_syntax<UserId>
    {
        protected override void ExecuteCommand(IEventStore store, ICommand<UserId> cmd)
        {
            new UserApplicationService(store).Execute(cmd);
        }

        protected override void SetupServices()
        {
            
        }
    }

}