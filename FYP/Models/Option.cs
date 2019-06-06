using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Option
    {
        public int OptionId { get; set; }
        public string OptionType { get; set; }
        public string OptionValue { get; set; }
        public int CurrentQuantity { get; set; }
        public int MinimumQuantity { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    }
}
