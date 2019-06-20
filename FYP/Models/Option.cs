using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Option
    {
        [Key]
        public int OptionId { get; set; }
        public string SKUNumber { get; set; }
        public string OptionType { get; set; }
        public string OptionValue { get; set; }
        public int CurrentQuantity { get; set; }
        public int MinimumQuantity { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; }
    }
}
