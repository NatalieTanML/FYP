using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Status
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }

        public List<Order> Orders { get; set; }
    }
}
