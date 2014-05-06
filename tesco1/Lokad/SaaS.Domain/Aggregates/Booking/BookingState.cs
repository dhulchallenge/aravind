using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaaS.Aggregates.Booking
{
    public sealed class BookingState:IBookingState
    {
        public BookingId Id { get; private set; }
        public Rental Booking { get; private set; }


        public BookingState(IEnumerable<IEvent> events)
        {
            Problems = new List<string>();
            foreach (var e in events)
            {
                Mutate(e);
            }
        }

        public IList<string> Problems { get; private set; }


        public void Mutate(IEvent e)
        {
            RedirectToWhen.InvokeEventOptional(this, e);
        }

        public void When(BookingCreated e)
        {
            Id = e.Id;
            Booking = e.Booking;
           
        }

      
    }
}
