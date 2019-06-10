using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Address
    {
        public int AddressId { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string UnitNo { get; set; }
        public string Country { get; set; }
        public string State { get; set; }

        public List<Order> Orders { get; set; }

        [ForeignKey("Hotel")]
        public int? HotelId { get; set; }
        public Hotel Hotel { get; set; }
    }
}
