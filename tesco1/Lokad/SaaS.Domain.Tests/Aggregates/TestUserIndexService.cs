#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sample;

namespace SaaS.Aggregates
{
    public sealed class TestUserIndexService<T> : IUserIndexService
        where T : IIdentity
    {
        readonly List<string> _records = new List<string>();

        bool IUserIndexService.IsLoginRegistered(string email)
        {
            return _records.Contains("email:" + email);
        }

        bool IUserIndexService.IsIdentityRegistered(string identity)
        {
            return _records.Contains("id:" + identity);
        }

        public IEvent<T> EmailRegistered(string email)
        {
            return new SpecSetupEvent<T>(
                () => _records.Add("email:" + email),
                "Setup IUserIndexService with email: " + email);
        }
        public IEvent<T> IdentityRegistered(string login)
        {
            return new SpecSetupEvent<T>(
                () => _records.Add("id:" + login),
                "Setup IUserIndexService with id: " + login);
        }
    }

    public sealed class TestSendEmail : IMailSender
    {
        bool _used;

        public void EnqueueText(Email[] to, string subject, string body, Email replyTo = null)
        {
            _used = true;
            Context.Explain("Send email to {0} '{1}' with body:\r\n{2}", string.Join(";", to.Select(s => s.ToString())),
                subject, body);
        }

        public void EnqueueHtml(Email[] to, string subject, string body, Email replyTo = null)
        {
            _used = true;
            Context.Explain("Send email to {0} '{1}' with body:\r\n{2}", string.Join(";", to.Select(s => s.ToString())),
                subject, body);
        }

        public override string ToString()
        {
            return _used ? "" : "Test mail sender";
        }
    }
}