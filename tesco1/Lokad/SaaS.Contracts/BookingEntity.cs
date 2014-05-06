using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;


namespace SaaS
{
    [DataContract]
    public class RentalBooking
    {
        [DataMember]
        public string rentalId { get; set; }

        [DataMember]
        public string rentalCarId { get; set; }
    }

}
