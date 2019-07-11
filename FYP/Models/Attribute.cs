using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class Attribute
    {
        public int AttributeId { get; set; }
        public string AttributeType { get; set; }
        public string AttributeValue { get; set; }

        public int OptionId { get; set; }
        public Option Option { get; set; }
    }
}
