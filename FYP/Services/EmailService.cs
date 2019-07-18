using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
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
        Task NotifyLowStock();
        Task CreateMessage(Enquiries enquiries);
        Task SendReceipt(Order newOrder);
    }

    public class EmailService : IEmailService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public EmailService(ApplicationDbContext context,
            IOptions<AppSettings> appSettings,
            IConfiguration configuration,
            IUserService userService)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _configuration = configuration;
            _userService = userService;
        }

        public async Task NotifyLowStock()
        {
            var users = await _userService.GetAll();

            var builder = new ConfigurationBuilder().SetBasePath
                (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("FYP", configuration["Email:Account"]));
            message.To.Add(new MailboxAddress("FYP", configuration["Email:Receiver"]));
            //foreach (string emails in emailAccounts)
            //{
            //     message.To.Add(new MailboxAddress("FYP", emails));
            // }
            foreach (User user in users)
            {
                message.To.Add(new MailboxAddress("FYP", user.Email));

            }
            message.Subject = "Test";
            message.Body = new TextPart("plain")
            {
                Text = "test yooo"
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                //client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                //Google
                await client.ConnectAsync("smtp.gmail.com", 587, false);
                await client.AuthenticateAsync(configuration["Email:Account"], configuration["Email:Password"]);

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

        public async Task CreateMessage(Enquiries enquiries)
        {
            Enquiries newEnquiries = new Enquiries()
            {
                name = enquiries.name,
                email = enquiries.email,
                message = enquiries.message
            };

            var builder = new ConfigurationBuilder().SetBasePath
                (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Memories@YourFingerTips", configuration["Email:Account"]));
            message.To.Add(new MailboxAddress("Admin_Memories@YourFingerTips", configuration["Email:Receiver"]));
            message.Subject = "Customer Message From Memories@YourFingerTips";
            message.Body = new TextPart("html")
            {
                Text =
                    "Hi," +
                    "<br>" + "<br>" +
                    newEnquiries.message +
                    "<br>" + "<br>" +
                    "<i>Customer Name: </i>" + newEnquiries.name +
                    "<br>" +
                    "<i>Customer Email: </i>" +
                     "<a href = 'mailto:" + newEnquiries.email +
                    "?subject=" + "New enquiries about Memories@YourFingerTips"
                    + "&body=" + "Hi " + newEnquiries.name + "'>"
                    + newEnquiries.email + "</a>" +
                    "<br>" + "<br>" +
                    "<i>This is a system generate email, please do not reply to this email.</i>" +
                    "<br>" +
                    "<i>Please click on customer email to start reply.</i>"
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                //client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                //Google
                await client.ConnectAsync("smtp.gmail.com", 587, false);
                await client.AuthenticateAsync(configuration["Email:Account"], configuration["Email:Password"]);

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

        public async Task SendReceipt(Order newOrder)
        {
            try
            {
                var builder = new ConfigurationBuilder().SetBasePath
                    (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
                var configuration = builder.Build();

                var email = DecryptString(newOrder.Email, configuration["Encryption:Key"]);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memory@YourFingerTips", configuration["Email:Account"]));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = "Thank You For Shopping at Memory@YourFingerTips";
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
                    // for attributes do a foreach (Attribute atr in itemProduct.Attributes), then atr.AttributeType and atr.AttributeValue
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
                    table += "<tr class='eachItem'><td width='80%' class='purchase_item'>" + "<img src='" + item.OrderImageUrl + "' alt='' width='80' height='80'>" + " " + itemProduct.Product.ProductName + itemAtr + "</td><td class='align-right' width='20%' class='purchase_item'>" + itemProduct.Product.Price.ToString("C", CultureInfo.CurrentCulture) + "</td></tr>";
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
                    text = text.Replace("<!-- SR content -->", "<table class='purchase' width='100%' cellpadding='0' cellspacing='0'><tr><td><h3>Special Request</h3></td></tr><tr><td colspan='2'><tr width='20%' class='deliveryAdd'><td width='40%' class='purchase_item'>" + newOrder.Request + "</td><tr><td width='20%' class='purchase_footer' valign='middle'></td></tr></td></tr></table>");

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
                    await client.ConnectAsync("smtp.gmail.com", 587, false);
                    await client.AuthenticateAsync(configuration["Email:Account"], configuration["Email:Password"]);

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
                throw new AppException("Unable to generate email.", new
                {
                    message = ex.Message
                });
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
