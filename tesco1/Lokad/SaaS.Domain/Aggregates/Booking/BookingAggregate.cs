using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SaaS.Aggregates.Booking
{
    public sealed class BookingAggregate
    {
        public void CreateBookingAggregate(BookingId bookingId,Rental booking)
        {
            Apply(new BookingCreated(bookingId,booking));
        }

        readonly BookingState _state;

        public IList<IEvent> Changes = new List<IEvent>();

        public BookingAggregate(BookingState state)
        {
            _state = state;
        }


        void Apply(IEvent<BookingId> e)
        {
            _state.Mutate(e);
            Changes.Add(e);
        }
    }
}
