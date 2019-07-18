using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAll();
        Task<IEnumerable<User>> GetDeliverymen();
        Task<User> GetById(int id);
        Task<User> Authenticate(string email, string password);
        Task<User> Create(User user);
        Task Update(User user, string password);
        Task Delete(int id);
        Task<IEnumerable<Role>> GetAllRoles();
        Task<User> ChangePassword(int id);
    }

    public class UserService : IUserService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;

        public UserService(ApplicationDbContext context, 
            IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await _context.Users.Include(user => user.Role).ToListAsync();
        }

        public async Task<IEnumerable<User>> GetDeliverymen()
        {
            return await _context.Users
                .Include(user => user.Role)
                .Where(user => user.RoleId == 2)
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetAllRoles()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<User> GetById(int id)
        {
            return await _context.Users.Include(user => user.Role).FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User> Authenticate(string inEmail, string inPassword)
        {
            if (string.IsNullOrWhiteSpace(inEmail) || string.IsNullOrWhiteSpace(inPassword))
                return null;

            // Check if there is matching username info first
            var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(x => x.Email == inEmail);
            if (user == null)
                return null;

            // if user != null, check if password matches
            if (!VerifyPasswordHash(inPassword, user.PasswordHash, user.PasswordSalt))
                return null;

            // authentication successful, 
            return user;
        }

        public async Task<User> Create(User user)
        {
            // If the user name (email) already exists, raise an exception
            // so that the Web API controller class code can capture the error and
            // send back a JSON response to the client side.
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                throw new AppException("Email '" + user.Email + "' is already in use.");

            user = await GenerateNewPasswordAndEmail(user, "Registration Successful!");

            // Update user details
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            user.IsEnabled = true;
            user.ChangePassword = false;
            
            // Add to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // returns user once done
            return user;
        }

        public async Task Update(User userParam, string inPassword)
        {
            var user = await _context.Users.FindAsync(userParam.UserId);

            if (user == null)
                throw new AppException("User not found.");

            // check whether the update request is from 
            // ChangePassword page or from UpdateDetails page 
            // if the email is empty/null/"", it means that the 
            // request is to change password only, therefore just update the password
            if (string.IsNullOrWhiteSpace(userParam.Email))
            {
                // update password if it was entered
                if (!string.IsNullOrWhiteSpace(inPassword))
                {
                    byte[] passwordHash, passwordSalt;
                    CreatePasswordHash(inPassword, out passwordHash, out passwordSalt);
                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    user.ChangePassword = true;
                }
            }
            // otherwise, if there are fields like email, it means the request
            // was sent from the UpdateDetails page from the user management pg
            else
            {
                if (userParam.Email != user.Email)
                {
                    // email has changed, check if new email is taken
                    if (await _context.Users.AnyAsync(x => x.Email == userParam.Email))
                        throw new AppException("Email " + userParam.Email + " is already in use.");
                    else
                        user.Email = userParam.Email;
                }
                // update user properties
                user.IsEnabled = userParam.IsEnabled;
                user.Name = userParam.Name;
                user.RoleId = userParam.RoleId;
                user.UpdatedAt = DateTime.Now;
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
            else
            {
                throw new AppException("User not found.");
            }
        }

        public async Task<User> ChangePassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            user = await GenerateNewPasswordAndEmail(user, "Reset Password");

            // Update user details
            user.UpdatedAt = DateTime.Now;
            user.ChangePassword = false;

            // Add to database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // returns user once done
            return user;
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
                inPasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
            }
        }

        private static bool VerifyPasswordHash(string inPassword, byte[] inStoredHash, byte[] inStoredSalt)
        {
            if (inPassword == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (inStoredHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (inStoredSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new HMACSHA512(inStoredSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != inStoredHash[i]) return false;
                }
            }

            return true;
        }

        private async Task<User> GenerateNewPasswordAndEmail(User user, string messageSubject)
        {
            //Generate random string for password.
            //interesting article https://stackoverflow.com/questions/37170388/create-a-cryptographically-secure-random-guid-in-net
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            var onebyte = new byte[16];
            rng.GetBytes(onebyte);
            string password = new Guid(onebyte).ToString("N");
            password = password.Substring(0, 11);

            // Create password hash & salt
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("WY", "weiyang35@hotmail.com"));
            message.To.Add(new MailboxAddress("WY", user.Email));
            message.Subject = messageSubject;
            message.Body = new TextPart("plain")
            {
                Text = "Your New Password: " + password
            };

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                //client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                //Google
                await client.ConnectAsync("smtp.office365.com", 587, false);
                await client.AuthenticateAsync("weiyang35@hotmail.com", "S9925187E");

                // Start of provider specific settings
                //Yhoo
                // client.Connect("smtp.mail.yahoo.com", 587, false);
                // client.Authenticate("yahoo", "password");

                // End of provider specific settings
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                client.Dispose();
            }

            return user;
        }
    }
}
