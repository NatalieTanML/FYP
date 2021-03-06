﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public bool IsEnabled { get; set; }
        public bool ChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [Column("CreatedBy")]
        public int? CreatedById { get; set; }
        public User CreatedBy { get; set; }
        
        public int RoleId { get; set; }
        public Role Role { get; set; }

        [NotMapped]
        public string Password { get; set; }

        public ICollection<Order> UpdatedOrders { get; set; }
        public ICollection<Order> Deliveries { get; set; }
        public ICollection<Product> CreatedProducts { get; set; }
        public ICollection<Product> UpdatedProducts { get; set; }
    }
}
