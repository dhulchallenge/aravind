using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using Lokad.Cqrs;
using Lokad.Cqrs.Evil;
using SaaS.Wires;
using ServiceStack.Text;
//using tesco.Commands;
//using tesco.Models;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace SaaS.Engine
{
    class Program
    {
        static void Main()
        {
            // CreateBookingTable();
            using (var env = BuildEnvironment())
            using (var cts = new CancellationTokenSource())
            {
                env.ExecuteStartupTasks(cts.Token);
                using (var engine = env.BuildEngine(cts.Token))
                {
                    var task = engine.Start(cts.Token);
                    //create a booking - test data 
                    Rental rent = new Rental { NumberOfDays = 11, RentalCarId = 1, RentalCustomer = new Customer { Address = "test", CustomerName = "name", Email = "test@test.com", Phone = "9500095000" }, StartDate = DateTime.UtcNow };

                     BookingCommand bComm = new BookingCommand(new BookingId( Guid.NewGuid().ToString()),rent);

                    // pass the booking id to update the booking details 
                   // BookingCommand bComm = new BookingCommand(new BookingId("Booking-03a3584d-69be-46d0-94e3-6ec364fa4ee3"), rent);
                    //env.SendToCommandRouter.Send(new CreateSecurityAggregate(new SecurityId(1)));


                    env.SendToCommandRouter.Send(bComm);//send a new booking to the queue

                    Console.WriteLine(@"Press enter to stop");
                    Console.ReadLine();
                    cts.Cancel();
                    if (!task.Wait(5000))
                    {
                        Console.WriteLine(@"Terminating");
                    }
                }
            }
        }

        private static void CreateBookingTable()
        {
            CloudStorageAccount cAccount = CloudStorageAccount.DevelopmentStorageAccount;
            CloudTableClient cClient = cAccount.CreateCloudTableClient();
            CloudTable cTable = cClient.GetTableReference("booking");
            cTable.CreateIfNotExists();

        }


        static void ConfigureObserver()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var observer = new ConsoleObserver();
            SystemObserver.Swap(observer);
            Context.SwapForDebug(s => SystemObserver.Notify(s));
        }

        public static Container BuildEnvironment()
        {
            //JsConfig.DateHandler = JsonDateHandler.ISO8601;
            ConfigureObserver();
            var integrationPath = AzureSettingsProvider.GetStringOrThrow(Conventions.StorageConfigName);
            //var email = AzureSettingsProvider.GetStringOrThrow(Conventions.SmtpConfigName);


            //var core = new SmtpHandlerCore(email);
            var setup = new Setup
            {
                //Smtp = core,
                //FreeApiKey = freeApiKey,
                //WebClientUrl = clientUri,
                //HttpEndpoint = endPoint,
                //EncryptorTool = new EncryptorTool(systemKey)
            };

            if (integrationPath.StartsWith("file:"))
            {
                var path = integrationPath.Remove(0, 5);

                SystemObserver.Notify("Using store : {0}", path);

                var config = FileStorage.CreateConfig(path);
                setup.Streaming = config.CreateStreaming();
                setup.DocumentStoreFactory = config.CreateDocumentStore;
                setup.QueueReaderFactory = s => config.CreateInbox(s, DecayEvil.BuildExponentialDecay(500));
                setup.QueueWriterFactory = config.CreateQueueWriter;
                setup.AppendOnlyStoreFactory = config.CreateAppendOnlyStore;

                setup.ConfigureQueues(1, 1);

                return setup.Build();
            }
            if (integrationPath.StartsWith("Default") || integrationPath.Equals("UseDevelopmentStorage=true", StringComparison.InvariantCultureIgnoreCase))
            {
                var config = AzureStorage.CreateConfig(integrationPath);
                setup.Streaming = config.CreateStreaming();
                setup.DocumentStoreFactory = config.CreateDocumentStore;
                setup.QueueReaderFactory = s => config.CreateQueueReader(s);
                setup.QueueWriterFactory = config.CreateQueueWriter;
                setup.AppendOnlyStoreFactory = config.CreateAppendOnlyStore;
                setup.ConfigureQueues(1);

                return setup.Build();
            }
            throw new InvalidOperationException("Unsupported environment");
        }
    }
}
