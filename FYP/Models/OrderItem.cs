using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string OrderImageUrl { get; set; }

        [NotMapped]
        public string OrderImageKey { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }
        
        public int OptionId { get; set; }
        public Option Option { get; set; }
    }
}
