using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class DeliveryType
    {
        public int DeliveryTypeId { get; set; }
        public string DeliveryTypeName { get; set; }

        public List<Order> Orders { get; set; }
    }
}
