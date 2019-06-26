﻿using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using FYP.Data;
using FYP.Helpers;
using FYP.Hubs;
using FYP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAll();
        Task<Order> GetById(int id);
        Task<Order> Create(Order order);
        Task UpdateStatus(int userId, int updatedById, int newStatus);
    }

    public class OrderService : IOrderService
    {
        private ApplicationDbContext _context;
        private readonly IOrderHub _orderHub;
        private readonly AppSettings _appSettings;
        private readonly IAmazonS3 _client;
        private readonly IConfiguration _configuration;

        private const string bucketName = "20190507test1";
        private const string FileName = "image3.jpg";
        private readonly string encryptionKey;
        
        public OrderService(ApplicationDbContext context, 
            IOrderHub orderHub, 
            IOptions<AppSettings> appSettings,
            IAmazonS3 client,
            IConfiguration configuration)
        {
            _context = context;
            _orderHub = orderHub;
            _appSettings = appSettings.Value;
            _client = client;
            _configuration = configuration;

            // get encryption key for email
            encryptionKey = _configuration.GetValue<string>("Encryption:Key");
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            List<Order> orders = await _context.Orders
                .Include(o => o.UpdatedBy)
                .Include(o => o.DeliveryType)
                .Include(o => o.Address)
                .ThenInclude(o => o.Hotel)
                .Include(o => o.Status)
                .Include(o => o.DeliveryMan)
                .Include(o => o.OrderRecipient)
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Option)
                .ThenInclude(o => o.Product)
                .ToListAsync();

            foreach (Order order in orders)
            {
                order.EmailString = DecryptString(order.Email, encryptionKey);
            }

            return orders;
        }

        public async Task<Order> GetById(int id)
        {
            var order = await _context.Orders
                .Include(o => o.UpdatedBy)
                .Include(o => o.DeliveryType)
                .Include(o => o.Address)
                .ThenInclude(o => o.Hotel)
                .Include(o => o.Status)
                .Include(o => o.DeliveryMan)
                .Include(o => o.OrderRecipient)
                .Include(o => o.OrderItems)
                .ThenInclude(o => o.Option)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            order.EmailString = DecryptString(order.Email, encryptionKey);

            return order;
        }

        public async Task<Order> Create(Order order)
        {
            try
            {
                // upload images to s3
                //await _s3Service.UploadImageAsync("https://20190507test1.s3-ap-southeast-1.amazonaws.com/image.jpg");

                // put the new images url into object
                foreach (OrderItem item in order.OrderItems)
                {
                    item.OrderImageUrl = "https://20190507test1.s3-ap-southeast-1.amazonaws.com/" + "image5" + ".jpg";
                }
                
                // create new order object to be added
                Order newOrder = new Order()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderSubtotal = decimal.Parse(order.OrderSubtotal.ToString()),
                    OrderTotal = decimal.Parse(order.OrderTotal.ToString()),
                    ReferenceNo = order.ReferenceNo,
                    Request = order.Request,
                    Email = EncryptString(order.EmailString, encryptionKey),
                    UpdatedById = order.UpdatedById,
                    DeliveryTypeId = order.DeliveryTypeId,
                    Address = order.Address,
                    StatusId = 1,
                    OrderItems = order.OrderItems
                };

                // add to database
                await _context.Orders.AddAsync(newOrder);
                await _context.SaveChangesAsync();
                await _orderHub.NotifyAllClients(newOrder);

                // returns product once done
                return newOrder;
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create product record.", new { message = ex.Message });
            }
        }

        public async Task UpdateStatus(int id, int updatedById, int newStatus)
        {
            var order = await _context.Orders.FindAsync(id);

            // if order does not exist
            if (order == null)
                throw new AppException("Order not found.");

            // update product status
            //switch(newStatus)
            //{
            //    // status "Awaiting Printing"
            //    case 2:
            //        order.StatusId = newStatus;
            //        break;
            //    // status "Printed"
            //    case 3:
            //        order.StatusId = newStatus
            //}
            
            order.UpdatedAt = DateTime.Now;
            order.UpdatedById = updatedById;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            await _orderHub.NotifyAllClients(order);
        }


        // private helper methods
        public static byte[] EncryptString(string text, string keyString)
        {
            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        var iv = aesAlg.IV;

                        var decryptedContent = msEncrypt.ToArray();

                        var result = new byte[iv.Length + decryptedContent.Length];

                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                        return result;
                    }
                }
            }
        }

        public static string DecryptString(byte[] cipherText, string keyString)
        {
            var fullCipher = cipherText;

            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, fullCipher.Length - iv.Length);

            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(cipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }
    }
}
