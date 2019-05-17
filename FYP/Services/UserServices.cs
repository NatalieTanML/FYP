﻿using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using MailKit;
using Org.BouncyCastle.Crypto.Tls;
using MailKit.Net.Smtp;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FYP.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAll();
        Task<User> GetById(int id);
        Task<User> Authenticate(string username, string password);
        Task<User> Create(User user); //string password);
        Task Update(User user, string password);
        Task Delete(int id);
    }

    public class UserService : IUserService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;

        public UserService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await _context.Users.Include(user => user.Role).ToListAsync();
        }

        public async Task<User> GetById(int id)
        {
            return await _context.Users.Include(user => user.Role).FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> Authenticate(string inUsername, string inPassword)
        {
            if (string.IsNullOrWhiteSpace(inUsername) || string.IsNullOrWhiteSpace(inPassword))
                return null;

            // Check if there is matching username info first
            var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(x => x.Username == inUsername);
            if (user == null)
                return null;

            // if user != null, check if password matches
            if (!VerifyPasswordHash(inPassword, user.PasswordHash, user.PasswordSalt))
                return null;

            // authentication successful, 
            return user;
        }

        public async Task<User> Create(User user)//string password)
        {
            // validation to check if the password is empty or spaces only.
            //if (string.IsNullOrWhiteSpace(password))
               // throw new AppException("Password is required");

            // If the user name (email) already exists, raise an exception
            // so that the Web API controller class code can capture the error and
            // send back a JSON response to the client side.
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                throw new AppException("Username " + user.Username + " is already taken");

            //Generate random password for user
            const int MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS = 2;
            const string LOWERCASE_CHARACTERS = "abcdefghijklmnopqrstuvwxyz";
            const string UPPERCASE_CHARACTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string NUMERIC_CHARACTERS = "0123456789";
            //Enable special characters and spaces here:
            //const string SPECIAL_CHARACTERS = @"!#$%&*@\";
            //const string SPACE_CHARACTER = " ";
            string characterSet = "";
            characterSet += LOWERCASE_CHARACTERS+ UPPERCASE_CHARACTERS+ NUMERIC_CHARACTERS; //+SPECIAL_CHARACTERS+SPACE_CHARACTER;

            //Change random password length here:
            char[] passwordRandom = new char[10];

            int characterSetLength = characterSet.Length;
            Random random = new Random();
            for (int characterPosition = 0; characterPosition < passwordRandom.Length; characterPosition++)
            {
                passwordRandom[characterPosition] = characterSet[random.Next(characterSetLength - 1)];

                bool moreThanTwoIdenticalInARow =
                    characterPosition > MAXIMUM_IDENTICAL_CONSECUTIVE_CHARS
                    && passwordRandom[characterPosition] == passwordRandom[characterPosition - 1]
                    && passwordRandom[characterPosition - 1] == passwordRandom[characterPosition - 2];

                if (moreThanTwoIdenticalInARow)
                {
                    characterPosition--;
                }
            }
            string password = string.Join(null, passwordRandom);

            // Create password hash & salt
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            // Add to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();



            //Generate email to user
            var builder = new ConfigurationBuilder().SetBasePath
                (Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Admin@kidzania", configuration["Email:Account"]));
            message.To.Add(new MailboxAddress("Admin@kidzania", configuration["Email:Receiver"]));
            message.Subject = "Administration account register successful";
            message.Body = new TextPart("plain")
            {
                Text = "Hi " + user.Username +",\n" + 
                "Your password:"+ password + 
                "\nPlease change your password"
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
            
            // returns user once done
            return user;
        }

        public async Task Update(User userParam, string inPassword)
        {
            var user = await _context.Users.FindAsync(userParam.UserId);

            if (user == null)
                throw new AppException("User not found.");

            if (userParam.Username != user.Username)
            {
                // username has changed, check if new username is taken
                if (await _context.Users.AnyAsync(x => x.Username == userParam.Username))
                    throw new AppException("Username " + userParam.Username + " is already taken.");
            }

            // update user properties
            user.Username = userParam.Username;
            user.IsEnabled = userParam.IsEnabled;

            // update password if it was entered
            if (!string.IsNullOrWhiteSpace(inPassword))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(inPassword, out passwordHash, out passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                try
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.ToUpper().Contains("REFERENCE CONSTRAINT"))
                        throw new AppException("Unable to delete user record. The user information might have been linked to other records.");
                    else
                        throw new AppException("Unable to delete user record.");
                }
            }
        }

        // private helper methods
        private static void CreatePasswordHash(string inPassword, out byte[] inPasswordHash, out byte[] inPasswordSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            //The password is hashed with a new random salt.
            //https://crackstation.net/hashing-security.htm
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                inPasswordSalt = hmac.Key;
                inPasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
            }
        }

        private static bool VerifyPasswordHash(string inPassword, byte[] inStoredHash, byte[] inStoredSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (inStoredHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (inStoredSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(inStoredSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != inStoredHash[i]) return false;
                }
            }

            return true;
        }

    }
}
