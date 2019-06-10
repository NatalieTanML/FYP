using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }
        public DateTime EffectiveStartDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("CreatedBy")]
        [Column("CreatedBy")]
        public int CreatedById { get; set; }
        public User CreatedBy { get; set; }

        [ForeignKey("UpdatedBy")]
        [Column("UpdatedBy")]
        public int UpdatedById { get; set; }
        public User UpdatedBy { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public List<DiscountPrice> DiscountPrices { get; set; }
        public List<ProductImage> ProductImages { get; set; }
        public List<Option> Options { get; set; }
    }
}
