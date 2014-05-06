using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cloud;
using Lokad.Cloud.Storage;


namespace SaaS.Aggregates.Booking
{
    public sealed class BookingApplicationService : IApplicationService, IBookingService
    {
        readonly IEventStore _eventStore;
        readonly Lokad.Cloud.Storage.CloudStorage.CloudStorageBuilder _provider;
        readonly CloudTable<Rental> _booking;
        readonly CloudTable<Car> _car;
        
        public BookingApplicationService(IEventStore eventStore)
        {
            _eventStore = eventStore;
            _provider = CloudStorage.ForDevelopmentStorage();
            _booking = new CloudTable<Rental>(_provider.BuildTableStorage(), "booking");
            try
            {
                _car = new CloudTable<Car>(_provider.BuildTableStorage(), "cardinals");
                
            }
            catch (Exception ex)
            {
                
                throw;
            }

        }

        public void Execute(object command)
        {
            RedirectToWhen.InvokeCommand(this, command);
        }
        public void When(BookingCommand c)
        {
            Update(c, aggregate => aggregate.CreateBookingAggregate(c.Id, c.Booking));
        }

        void Update(BookingCommand c, Action<BookingAggregate> action)
        {
            try
            {
               
                var stream = _eventStore.LoadEventStream(c.Id);
                var state = new BookingState(stream.Events);
                var agg = new BookingAggregate(state);
                SaveBooking(c);//save booking to table storage.x
                using (Context.CaptureForThread())
                {
                    action(agg);
                    _eventStore.AppendEventsToStream(c.Id, stream.StreamVersion, agg.Changes);
                }
            }
            catch (Exception ex )
            {
                
                throw ex;
            }
        }

        /// <summary>
        /// save the booking to table storage.
        /// </summary>
        /// <param name="c"></param>
        private void SaveBooking(BookingCommand c)
        {

            _booking.Upsert(

                new CloudEntity<Rental>
                {
                    PartitionKey = c.Booking.RentalCarId.ToString(),
                    RowKey = c.Id.ToString(),
                    Timestamp = DateTime.UtcNow,
                    Value = c.Booking


                }
            );

            var test = _booking.Get(c.Booking.RentalCarId.ToString(), c.Id.ToString());
        }

        /// <summary>
        /// check availability of given car model
        /// </summary>
        /// <param name="CarModel"></param>
        /// <returns>true or false </returns>
        public bool CheckAvailability(string CarModel)
        {
           return _car.Get(CarModel).Where(c=>c.Value.IsAvailable == true).FirstOrDefault()==null ? false: true; 
        }
    }
}
