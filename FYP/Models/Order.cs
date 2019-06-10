using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        [ForeignKey("UpdatedBy")]
        [Column("UpdatedBy")]
        public int? UpdatedById { get; set; }
        public User UpdatedBy { get; set; }

        [ForeignKey("DeliveryType")]
        public int DeliveryTypeId { get; set; }
        public DeliveryType DeliveryType { get; set; }

        [ForeignKey("Address")]
        public int? AddressId { get; set; }
        public Address Address { get; set; }

        [ForeignKey("Status")]
        public int StatusId { get; set; }
        public Status Status { get; set; }
        
        [ForeignKey("DeliveryMan")]
        public int? DeliveryManId { get; set; }
        public User DeliveryMan { get; set; }

        [ForeignKey("OrderRecipient")]
        public int? OrderRecipientId { get; set; }
        public OrderRecipient OrderRecipient { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    }
}
