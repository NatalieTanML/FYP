using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace FYP.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private IUserService _userService;
        private readonly AppSettings _appSettings;
        //private ArrayList emailAccounts = new ArrayList();

        public EmailController(
            IUserService userService,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _appSettings = appSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAll();

            var builder = new ConfigurationBuilder().SetBasePath
                (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("FYP", configuration["Email:Account"]));
            message.To.Add(new MailboxAddress("FYP", configuration["Email:Receiver"]));

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

            return Ok(new
            {
                message = "Done"
            });
        }

        [HttpPost("message")]
        public async Task<IActionResult> CreateMessage([FromBody]Enquiries enquiries)
        {

            try
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
                message.From.Add(new MailboxAddress("Memory@YourFingerTips", configuration["Email:Account"]));
                message.To.Add(new MailboxAddress("Admin_Memory@YourFingerTips", configuration["Email:Receiver"]));
                message.Subject = "Customer Message From Memory@YourFingerTips";
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

                return Ok(new
                {
                    message = "Done"
                });

            }
            catch (Exception ex)
            {
                throw new AppException("Unable to send message.", new
                {
                    message = ex.Message
                });
            }
        }


        [HttpPost("receipt")]
        public async Task<IActionResult> Receipt([FromBody]Enquiries enquiries)
        {

            try
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
                message.From.Add(new MailboxAddress("Memory@YourFingerTips", configuration["Email:Account"]));
                message.To.Add(new MailboxAddress(newEnquiries.name, configuration["Email:Receiver"]));
                message.Subject = "Thank You For Shopping at Memory@YourFingerTips";

                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"EmailTemplate\template.txt");
                string text = System.IO.File.ReadAllText(path);
                text = text.Replace("{{name}}", newEnquiries.name);
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

                return Ok(new
                {
                    message = "Done"
                });

            }
            catch (Exception ex)
            {
                throw new AppException("Unable to generate email.", new
                {
                    message = ex.Message
                });
            }
        }


    }
}