using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                newEnquiries.name +
                "<br>" + "<br>" +
                "<i>This is a system generated email, please do not reply to this email.</i>" +
                "<br>" +
                "<i>Please click on customer email to start reply </i>" +
                "<a href = 'mailto:" + newEnquiries.email +
                "?subject=" + "Thank you for contacting Memories@YourFingerTips"
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
        }
    }
}
