#region (c) 2010-2012 Lokad - CQRS Sample for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2012, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs.AtomicStorage;
using SaaS.Aggregates.Register;
using SaaS.Aggregates.Security;
using SaaS.Aggregates.User;
using SaaS.Aggregates.Booking;
using SaaS.Processes;
using SaaS.Services.UserIndex;

namespace SaaS
{
    public static class DomainBoundedContext
    {
        public static string EsContainer = "hub-domain-tape";

        public static IEnumerable<object> Projections(IDocumentStore docs)
        {
            yield return new UserIndexProjection(docs.GetWriter<byte, UserIndexLookup>());
        }

        public static IEnumerable<Func<CancellationToken, Task>> Tasks(ICommandSender service, IDocumentStore docs,
            bool isTest)
        {
            var flow = new DomainSender(service);
            // more tasks go here
            yield break;
        }


        public static IEnumerable<object> Ports(ICommandSender service)
        {
            var flow = new DomainSender(service);
            yield return new ReplicationPort(flow);
            yield return new RegistrationPort(flow);
            // more senders go here
        }

        public static IEnumerable<object> EntityApplicationServices(IDocumentStore docs, IEventStore store, DomainIdentityGenerator id)
        {
            var unique = new UserIndexService(docs.GetReader<byte, UserIndexLookup>());
            var passwords = new PasswordGenerator();

            yield return new BookingApplicationService(store);

            //yield return new UserApplicationService(store);
            //yield return new SecurityApplicationService(store, id, passwords, unique);
            //yield return new RegistrationApplicationService(store, id, unique, passwords);
            yield return id;
        }
        public static IEnumerable<object> FuncApplicationServices()
        {
            yield break;
            
        } 
    }
}