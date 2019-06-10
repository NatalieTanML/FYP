﻿using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<DeliveryType> DeliveryTypes { get; set; }
        public DbSet<DiscountPrice> DiscountPrices { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderRecipient> OrderRecipients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Status> Status { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.Relational().TableName = entity.DisplayName();
            }

            //------------- User - Start -------------

            modelBuilder.Entity<User>()
                .HasOne(input => input.Role)
                .WithMany()
                .HasForeignKey(input => input.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            //------------- User - End -------------

            //------------- Product - Start -------------

            modelBuilder.Entity<Product>()
                .HasOne(input => input.Category)
                .WithMany(input => input.Products)
                .HasForeignKey(input => input.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(a => a.CreatedBy)
                .WithOne()
                .HasForeignKey<User>()
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<Product>()
                .HasOne(a => a.UpdatedBy)
                .WithOne()
                .HasForeignKey<User>()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.DiscountPrices)
                .WithOne(p => p.Product)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.ProductImages)
                .WithOne(p => p.Product)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Options)
                .WithOne(p => p.Product)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            //------------- Product - End -------------

            //------------- Order - Start -------------

            modelBuilder.Entity<Order>()
                .HasOne(a => a.UpdatedBy)
                .WithOne()
                .HasForeignKey<User>()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(a => a.DeliveryType)
                .WithMany(a => a.Orders)
                .HasForeignKey(a => a.DeliveryTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .HasOne(a => a.Address)
                .WithMany(a => a.Orders)
                .HasForeignKey(b => b.AddressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(a => a.Status)
                .WithMany(a => a.Orders)
                .HasForeignKey(a => a.StatusId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .HasOne(a => a.DeliveryMan)
                .WithMany()
                .HasForeignKey(b => b.DeliveryManId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasOne(a => a.OrderRecipient)
                .WithMany(a => a.Orders)
                .HasForeignKey(b => b.OrderRecipientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            //------------- Order - End -------------

            modelBuilder.Entity<Hotel>()
                .HasMany(h => h.Addresses)
                .WithOne(a => a.Hotel)
                .HasForeignKey(a => a.HotelId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
