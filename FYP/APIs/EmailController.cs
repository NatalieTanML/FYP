using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.IO;
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

        [HttpPost]
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
                    newEnquiries.name +
                    "<br>" + "<br>" +
                    "<i>This is a system generate email, please do not reply to this email.</i>" +
                    "<br>" +
                    "<i>Please click on customer email to start reply </i>" +
                    "<a href = 'mailto:" + newEnquiries.email +
                    "?subject=" + "Thank you for contacting Memory@YourFingerTips"
                    + "&body=" + "Hi " + newEnquiries.name + "'>"
                    + newEnquiries.email + "</a>"
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
                throw new AppException("Unable to create product record.", new
                {
                    message = ex.Message
                });
            }
        }
    }
}