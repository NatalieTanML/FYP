using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal OrderSubtotal { get; set; }
        public decimal OrderTotal { get; set; }
        public string ReferenceNo { get; set; }
        public string Request { get; set; }
        public byte[] Email { get; set; }

        public int UpdatedBy { get; set; }
        public User User { get; set; }
        public string Username { get; set; }

        public int DeliveryTypeId { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public string DeliveryTypeName { get; set; }

        public int AddressId { get; set; }
        public Address Address { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }
        public string StatusName { get; set; }
    }
}
