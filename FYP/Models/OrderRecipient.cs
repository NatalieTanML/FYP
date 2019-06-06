using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class OrderRecipient
    {
        public int OrderRecipientId { get; set; }
        public string ReceivedBy { get; set; }
        public byte[] RecipientSignature { get; set; }
        public DateTime ReceivedAt { get; set; }

        public List<Order> Orders { get; set; }
    }
}
