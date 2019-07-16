using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IEmailService
    {
        Task NotifyLowStock();
        Task CreateMessage(Enquiries enquiries);
    }

    public class EmailService : IEmailService
    {
        private readonly AppSettings _appSettings;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public EmailService(ApplicationDbContext context,
            IOptions<AppSettings> appSettings,
            IConfiguration configuration,
            IUserService userService)
        {
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
                    "?subject=" + "Thank you for contacting Memory@YourFingerTips"
                    + "&body=" + "Hi " + newEnquiries.name + "'>"
                    + newEnquiries.email + "</a>" +
                    "<br>" +
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

        //public async Task SendEmailToUser(User user, string password, string messageSubject)
        //{
        //    var message = new MimeMessage();
        //    message.From.Add(new MailboxAddress("WY", "weiyang35@hotmail.com"));
        //    message.To.Add(new MailboxAddress("WY", user.Email));
        //    message.Subject = messageSubject;
        //    message.Body = new TextPart("plain")
        //    {
        //        Text = "Your New Password: " + password
        //    };

        //    using (var client = new MailKit.Net.Smtp.SmtpClient())
        //    {
        //        //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        //        //client.AuthenticationMechanisms.Remove("XOAUTH2");
        //        client.ServerCertificateValidationCallback = (s, c, h, e) => true;

        //        //Google
        //        await client.ConnectAsync("smtp.office365.com", 587, false);
        //        await client.AuthenticateAsync("weiyang35@hotmail.com", "S9925187E");

        //        // Start of provider specific settings
        //        //Yhoo
        //        // client.Connect("smtp.mail.yahoo.com", 587, false);
        //        // client.Authenticate("yahoo", "password");

        //        // End of provider specific settings
        //        await client.SendAsync(message);
        //        await client.DisconnectAsync(true);
        //        client.Dispose();
        //    }
        //}

        public async Task Receipt(Order newOrder)
        {

            try
            {
                var builder = new ConfigurationBuilder().SetBasePath
                    (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
                var configuration = builder.Build();

                var order = newOrder;
                var email = DecryptString(order.Email, configuration["Encryption:Key"]);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Memory@YourFingerTips", configuration["Email:Account"]));
                message.To.Add(new MailboxAddress(email, email));
                //message.To.Add(new MailboxAddress("hi", "jingsong0102@gmail.com"));
                message.Subject = "Thank You For Shopping at Memory@YourFingerTips";
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"EmailTemplate\template.txt");
                string text = System.IO.File.ReadAllText(path);
                text = text.Replace("{{receipt_id}}", order.ReferenceNo);
                text = text.Replace("{{date}}", order.CreatedAt.ToLongDateString());
                var table = "";
                foreach (OrderItem item in order.OrderItems)
                {

                    table += "<tr class='eachItem'><td width='80%' class='purchase_item'>" + item.Option.Product.ProductName + "</td><td class='align-right' width='20%' class='purchase_item'>{{amount}}</td></tr>";

                }
                text = text.Replace(
                "<tr class='eachItem'><td width='40%' class='purchase_item'>{{description}}</td><td class='align-right' width='20%' class='purchase_item'>{{amount}}</td></tr>",
                    table);
                text = text.Replace("{{total}}", order.OrderTotal.ToString("C", CultureInfo.CurrentCulture));

                System.IO.File.WriteAllText(path, text);
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
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate(configuration["Email:Account"], configuration["Email:Password"]);

                    // Start of provider specific settings
                    //Yhoo
                    // client.Connect("smtp.mail.yahoo.com", 587, false);
                    // client.Authenticate("yahoo", "password");

                    // End of provider specific settings
                    client.Send(message);
                    client.Disconnect(true);
                    client.Dispose();
                }

                //return Ok(new
                // {
                //     message = "Done"
                // });

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
