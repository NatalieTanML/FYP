using Amazon.S3;
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
        Task UpdateStatus(int userId, Order inOrder);
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
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            return await _context.Orders
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
        }

        public async Task<Order> GetById(int id)
        {
            return await _context.Orders
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
                    item.OrderImageUrl = "new url";
                }

                // get encryption key for email
                string encryptionKey = _configuration.GetValue<string>("Encryption:Key");

                // create new order object to be added
                Order newOrder = new Order()
                {
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderSubtotal = decimal.Parse(order.OrderSubtotal.ToString()),
                    OrderTotal = decimal.Parse(order.OrderTotal.ToString()),
                    ReferenceNo = "12345678", // to be generated
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
                await _orderHub.NotifyAllClients();

                // returns product once done
                return newOrder;
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create product record.", new { message = ex.Message });
            }
        }

        public async Task UpdateStatus(int id, Order inOrder)
        {
            var order = await _context.Orders.FindAsync(id);

            // if order does not exist
            if (order == null)
                throw new AppException("Order not found.");

            // update product status
            //order.Status = inOrder.Status;
            order.StatusId = inOrder.StatusId;
            order.UpdatedAt = DateTime.Now;
            order.UpdatedById = inOrder.UpdatedById;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            await _orderHub.NotifyAllClients();
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

        public static string DecryptString(string cipherText, string keyString)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

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
