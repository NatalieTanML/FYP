using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Hotel
    {
        public int HotelId;
        public string HotelName;
        public string HotelAddress;
        public string HotelPostalCode;

        public List<Address> Addresses { get; set; }
    }
}
