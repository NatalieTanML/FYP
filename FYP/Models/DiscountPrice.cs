using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class DiscountPrice
    {
        public int DiscountPriceId { get; set; }
        public DateTime EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public decimal DiscountValue { get; set; }
        public bool IsPercentage { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}
