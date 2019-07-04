﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public string ImageKey { get; set; }
        public string ImageUrl { get; set; }

        [NotMapped]
        public IFormFile ImageFile { get; set; }
        
        public int OptionId { get; set; }
        public Option Option { get; set; }
    }
}
