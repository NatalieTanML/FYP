using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public decimal OrderSubtotal { get; set; }
        public decimal OrderTotal { get; set; }
        public string ReferenceNo { get; set; }
        public string Request { get; set; }
        public byte[] Email { get; set; }

        [NotMapped]
        public string EmailString { get; set; }

        [Column("UpdatedBy")]
        public int? UpdatedById { get; set; }
        public User UpdatedBy { get; set; }

        public int DeliveryTypeId { get; set; }
        public DeliveryType DeliveryType { get; set; }

        public int? AddressId { get; set; }
        public Address Address { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }
        
        public int? DeliveryManId { get; set; }
        public User DeliveryMan { get; set; }

        public int? OrderRecipientId { get; set; }
        public OrderRecipient OrderRecipient { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}
