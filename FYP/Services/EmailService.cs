using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Attribute = FYP.Models.Attribute;

namespace FYP.Services
{
    public interface IEmailService
    {
        Task<User> GenerateNewPasswordAndEmail(User user, string messageSubject);
        Task NotifyLowStock(List<Option> options);
        Task CreateMessage(Enquiries enquiries);
        Task SendReceipt(Order newOrder);
    }

    public class EmailService : IEmailService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly IProductService _productService;
        private readonly string emailAccount, emailPassword, emailReceiver, emailHost, encryptionKey;
        private readonly int emailPort;

        public EmailService(ApplicationDbContext context,
            IOptions<AppSettings> appSettings,
            IProductService productService)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _productService = productService;

            emailAccount = Environment.GetEnvironmentVariable("Email:Account");
            emailPassword = Environment.GetEnvironmentVariable("Email:Password");
            emailReceiver = Environment.GetEnvironmentVariable("Email:Receiver");
            emailHost = Environment.GetEnvironmentVariable("Email:Host");
            emailPort = int.Parse(Environment.GetEnvironmentVariable("Email:Port"));
            encryptionKey = Environment.GetEnvironmentVariable("Encryption:Key");
        }

        public async Task<User> GenerateNewPasswordAndEmail(User user, string messageSubject)
        {
            try
            {
                //Generate random string for password.
                //interesting article https://stackoverflow.com/questions/37170388/create-a-cryptographically-secure-random-guid-in-net
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                var onebyte = new byte[16];
                rng.GetBytes(onebyte);
                string password = new Guid(onebyte).ToString("N");
                password = password.Substring(0, 11);

                // Create password hash & salt
                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memories @ Your Fingertips", emailAccount));
                message.To.Add(new MailboxAddress(user.Name, user.Email));
                message.Subject = messageSubject;
                message.Body = new TextPart("html")
                {
                    Text = "Your new password: " + password
                        + "<br><br><i>This is a system-generated email. Please do not reply to this email.</i>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    //Google
                    await client.ConnectAsync(emailHost, emailPort, false);
                    await client.AuthenticateAsync(emailAccount, emailPassword);

                    // Start of provider specific settings
                    //Yhoo
                    // client.Connect("smtp.mail.yahoo.com", 587, false);
                    // client.Authenticate("yahoo", "password");

                    // End of provider specific settings
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new AppException(ex.Message);
            }
            return user;
        }

        public async Task NotifyLowStock(List<Option> options)
        {
            try
            {
                // get all store users
                var users = await _context.Users
                    .Where(u => u.RoleId == 1)
                    .ToListAsync();

                if (users == null)
                    throw new AppException("Unable to retrieve users. Please try again.");

                if (options == null)
                    throw new AppException("Options/SKUs do not exist.");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memories @ Your FingerTips", emailAccount));
                message.Subject = "Low Stock Notification";
                foreach (User user in users)
                {
                    message.To.Add(new MailboxAddress(user.Name, user.Email));
                }
                string SKUs = "";
                foreach (Option o in options)
                {
                    SKUs += "<b>'" + o.SKUNumber + "'</b> ";
                }

                message.Body = new TextPart("html")
                {
                    Text = "Stock count for " + SKUs + "is low."
                        + "<br>Please restock the inventory for the item(s), and update the quantity in Resource Management."
                        + "<br><i>This is a system-generated email. Please do not reply to this email.</i>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    //Google
                    await client.ConnectAsync(emailHost, emailPort, false);
                    await client.AuthenticateAsync(emailAccount, emailPassword);

                    // Start of provider specific settings
                    //Yhoo
                    // client.Connect("smtp.mail.yahoo.com", 587, false);
                    // client.Authenticate("yahoo", "password");

                    // End of provider specific settings
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new AppException(ex.Message);
            }
        }

        public async Task CreateMessage(Enquiries enquiries)
        {
            try
            {
                Enquiries newEnquiries = new Enquiries()
                {
                    name = enquiries.name,
                    email = enquiries.email,
                    message = enquiries.message
                };
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memories @ Your Fingertips", emailAccount));
                message.To.Add(new MailboxAddress("Memories @ Your Fingertips", emailReceiver));
                message.Subject = "Message from Customer (Memories @ Your Fingertips)";
                message.Body = new TextPart("html")
                {
                    Text =
                        "New message received from a customer: " +
                        "<br>" + "<br>" +
                        newEnquiries.message +
                        "<br>" + "<br>" +
                        "<i>Customer Name: </i>" + newEnquiries.name +
                        "<br>" +
                        "<i>Customer Email: </i>" +
                         "<a href = 'mailto:" + newEnquiries.email +
                        "?subject=" + "Reply from Memories @ Your Fingertips"
                        + "&body=" + "Hi " + newEnquiries.name + "'>"
                        + newEnquiries.email + "</a>" +
                        "<br>" + "<br>" +
                        "<i>This is a system generated email, please do not reply to this email.</i>" +
                        "<br>" +
                        "<i>Please click on customer email to reply.</i>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    //Google
                    await client.ConnectAsync(emailHost, emailPort, false);
                    await client.AuthenticateAsync(emailAccount, emailPassword);

                    // Start of provider specific settings
                    //Yhoo
                    // client.Connect("smtp.mail.yahoo.com", 587, false);
                    // client.Authenticate("yahoo", "password");

                    // End of provider specific settings
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new AppException(ex.Message);
            }
        }

        public async Task SendReceipt(Order newOrder)
        {
            try
            {
                var email = DecryptString(newOrder.Email, encryptionKey);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memories @ Your Fingertips", emailAccount));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "Your order receipt for 'Memories @ Your Fingertips'";
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"EmailTemplate\receiptTemplate.txt");
                string text = File.ReadAllText(path);
                text = text.Replace("{{OrderRef}}", newOrder.ReferenceNo);
                text = text.Replace("{{receipt_id}}", newOrder.ReferenceNo);
                text = text.Replace("{{date}}", newOrder.CreatedAt.ToLongDateString());
                var table = "";
                foreach (OrderItem item in newOrder.OrderItems)
                {
                    var itemProduct = await _context.Options
                        .Where(o => o.OptionId == item.OptionId)
                        .Include(o => o.Attributes)
                        .Include(o => o.Product)
                        .FirstOrDefaultAsync();
                    var itemAtr = " ";
                    var c = 1;

                    foreach (Attribute atr in itemProduct.Attributes)
                    {
                        if (itemProduct.Attributes.Count > 1)
                        {
                            if (c == itemProduct.Attributes.Count)
                                itemAtr += atr.AttributeValue + ")";
                            else itemAtr += "(" + atr.AttributeValue + ",";
                            c++;
                        }
                        else itemAtr += "(" + atr.AttributeValue + ")";
                    }

                    var product = await _productService.GetById(itemProduct.Product.ProductId);
                    var effectiveDiscountPrice = _productService.RetrieveEffectiveDiscount(product);
                    decimal itemPrice;
                    if (effectiveDiscountPrice.Count != 0)
                    {
                        var discountPrice = effectiveDiscountPrice.ElementAt(0).ToString();
                        if (discountPrice.Contains("True"))
                        {
                            int pFrom = discountPrice.IndexOf("discountPrice = ") + "discountPrice = ".Length;
                            int pTo = discountPrice.LastIndexOf(" }");
                            itemPrice = Decimal.Parse(discountPrice.Substring(pFrom, pTo - pFrom));
                        }
                        else
                        {
                            int pFrom = discountPrice.IndexOf("discountValue = ") + "discountValue = ".Length;
                            int pTo = discountPrice.LastIndexOf(", IsPercentage");
                            itemPrice = Decimal.Parse(discountPrice.Substring(pFrom, pTo - pFrom));
                        }
                    }
                    else itemPrice = itemProduct.Product.Price;
                    table += "<tr class='eachItem'><td width='80%' class='purchase_item'>"
                    + "<img src='" + item.OrderImageUrl + "' alt='' width='80' height='80'>"
                    + " " + itemProduct.Product.ProductName + itemAtr
                    + "</td><td class='align-right' width='20%' class='purchase_item'>"
                    + itemPrice.ToString("C", CultureInfo.CurrentCulture) + " x " + item.Quantity + "</td></tr>";

                }
                text = text.Replace(
                "<tr class='eachItem'><td width='40%' class='purchase_item'>{{description}}</td><td class='align-right' width='20%' class='purchase_item'>{{amount}}</td></tr>",
                    table);
                text = text.Replace("{{total}}", newOrder.OrderTotal.ToString("C", CultureInfo.CurrentCulture));

                if (newOrder.DeliveryTypeId == 3)
                    text = text.Replace("{{ShippingAddress}}",
                        "Self-Pickup");
                if (newOrder.DeliveryTypeId == 1)
                    text = text.Replace("{{ShippingAddress}}",
                        newOrder.Address.UnitNo + "<br>"
                        + newOrder.Address.AddressLine2
                        + "<br>" + newOrder.Address.AddressLine1
                        + "<br>" + newOrder.Address.PostalCode +
                        "<br>" + newOrder.Address.State +
                        "<br>" + newOrder.Address.Country);
                if (newOrder.DeliveryTypeId == 2)
                {
                    if (newOrder.Address.AddressLine2 == "")
                        text = text.Replace("{{ShippingAddress}}",
                                newOrder.Address.UnitNo + "<br>" +
                                newOrder.Address.AddressLine1 +
                                "<br>" + newOrder.Address.PostalCode
                                + "<br>" + newOrder.Address.State + "<br>"
                                + newOrder.Address.Country);
                    else text = text.Replace("{{ShippingAddress}}",
                            newOrder.Address.UnitNo + "<br>" +
                            newOrder.Address.AddressLine1 +
                            "<br>" + newOrder.Address.AddressLine2
                            + "<br>" + newOrder.Address.PostalCode
                            + "<br>" + newOrder.Address.State + "<br>"
                            + newOrder.Address.Country);
                }
                if (newOrder.Request != "")
                    text = text.Replace("<!-- SR content -->",
                        "<table class='purchase' width='100%' cellpadding='0' cellspacing='0'><tr><td>" +
                        "<h3>Special Request</h3></td></tr><tr><td colspan='2'><tr width='20%' class='deliveryAdd'>" +
                        "<td width='40%' class='purchase_item'>" + newOrder.Request +
                        "</td><tr><td width='20%' class='purchase_footer' valign='middle'></td></tr></td></tr></table>");

                message.Body = new TextPart("html")
                {
                    Text = text
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    //Google
                    await client.ConnectAsync(emailHost, emailPort, false);
                    await client.AuthenticateAsync(emailAccount, emailPassword);

                    // Start of provider specific settings
                    //Yhoo
                    // client.Connect("smtp.mail.yahoo.com", 587, false);
                    // client.Authenticate("yahoo", "password");

                    // End of provider specific settings
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to generate email.", ex.Message);
            }
        }

        // private helper methods
        private static void CreatePasswordHash(string inPassword, out byte[] inPasswordHash, out byte[] inPasswordSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            //The password is hashed with a new random salt.
            //https://crackstation.net/hashing-security.htm
            using (var hmac = new HMACSHA512())
            {
                inPasswordSalt = hmac.Key;
                inPasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(inPassword));
            }
        }

        private static string DecryptString(byte[] cipherText, string keyString)
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
