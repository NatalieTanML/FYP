//using FYP.Data;
//using FYP.Helpers;
//using FYP.Models;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;
//using MimeKit;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Threading.Tasks;

//namespace FYP.Services
//{
//    public interface IUserService
//    {
//        Task<IEnumerable<User>> GetAll();
//        Task<User> GetById(int id);
//        Task<User> Authenticate(string username, string password);
//        Task<User> Create(User user);
//        Task Update(User user, string password);
//        Task Delete(int id);
//    }

//    public class UserService : IUserService
//    {
//        private ApplicationDbContext _context;
//        private readonly AppSettings _appSettings;

//        public UserService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
//        {
//            _context = context;
//            _appSettings = appSettings.Value;
//        }

//        public async Task<IEnumerable<User>> GetAll()
//        {
//            return await _context.Users.Include(user => user.Role).ToListAsync();
//        }

//        public async Task<User> GetById(int id)
//        {
//            return await _context.Users.Include(user => user.Role).FirstOrDefaultAsync(u => u.UserId == id);
//        }

//        public async Task<User> Authenticate(string inUsername, string inPassword)
//        {
//            if (string.IsNullOrWhiteSpace(inUsername) || string.IsNullOrWhiteSpace(inPassword))
//                return null;

//            // Check if there is matching username info first
//            var user = await _context.Users.Include(u => u.Role).SingleOrDefaultAsync(x => x.Username == inUsername);
//            if (user == null)
//                return null;

//            // if user != null, check if password matches
//            if (!VerifyPasswordHash(inPassword, user.PasswordHash, user.PasswordSalt))
//                return null;

//            // authentication successful, 
//            return user;
//        }

//        public async Task<User> Create(User user)
//        {
//            // validation to check if the password is empty or spaces only.
        

//            // If the user name (email) already exists, raise an exception
//            // so that the Web API controller class code can capture the error and
//            // send back a JSON response to the client side.
//            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
//                throw new AppException("Username " + user.Username + " is already taken");


//            //Generate random string for password.
//            //interesting article https://stackoverflow.com/questions/37170388/create-a-cryptographically-secure-random-guid-in-net
//            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

//            var onebyte = new byte[16];
//            rng.GetBytes(onebyte);
//            Guid guid = new Guid(onebyte);
//            string password = guid.ToString("N");
//            password = password.Substring(0,11);

//            // Create password hash & salt
//            byte[] passwordHash, passwordSalt;
//            CreatePasswordHash(password, out passwordHash, out passwordSalt);
//            user.PasswordHash = passwordHash;
//            user.PasswordSalt = passwordSalt;

//            var message = new MimeMessage();
//            message.From.Add(new MailboxAddress("WY", "weiyang35@hotmail.com"));
//            message.To.Add(new MailboxAddress("WY", user.Username));
//            message.Subject = "Registration successful";
//            message.Body = new TextPart("plain")
//            {
//                Text = "Your New Password: " + password
//            };

//            using (var client = new MailKit.Net.Smtp.SmtpClient())
//            {

//                //client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
//                //client.AuthenticationMechanisms.Remove("XOAUTH2");
//                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

//                //Google
//                client.Connect("smtp.office365.com", 587, false);
//                client.Authenticate("weiyang35@hotmail.com", "S9925187E");

//                // Start of provider specific settings
//                //Yhoo
//                // client.Connect("smtp.mail.yahoo.com", 587, false);
//                // client.Authenticate("yahoo", "password");

//                // End of provider specific settings
//                client.Send(message);
//                client.Disconnect(true);
//                client.Dispose();
//            }

//            // Add to database
//            _context.Users.Add(user);
//            await _context.SaveChangesAsync();
            
//            //Generate email to user
           

//            // returns user once done
//            return user;
//        }

//        public async Task Update(User userParam, string inPassword)
//        {
//            var user = await _context.Users.FindAsync(userParam.UserId);

//            if (user == null)
//                throw new AppException("User not found.");

//            if (userParam.Username != user.Username)
//            {
//                // username has changed, check if new username is taken
//                if (await _context.Users.AnyAsync(x => x.Username == userParam.Username))
//                    throw new AppException("Username " + userParam.Username + " is already taken.");
//            }

//            // update user properties
           
//            user.IsEnabled = userParam.IsEnabled;
//            user.ChangePassword = true;
//            // update password if it was entered
//            if (!string.IsNullOrWhiteSpace(inPassword))
//            {
//                byte[] passwordHash, passwordSalt;
//                CreatePasswordHash(inPassword, out passwordHash, out passwordSalt);
//                user.PasswordHash = passwordHash;
//                user.PasswordSalt = passwordSalt;
//            }

//            _context.Users.Update(user);
//            await _context.SaveChangesAsync();
//        }

//        public async Task Delete(int id)
//        {
//            var user = await _context.Users.FindAsync(id);
//            if (user != null)
//            {
//                try
//                {
//                    _context.Users.Remove(user);
//                    await _context.SaveChangesAsync();
//                }
//                catch (Exception ex)
//                {
//                    if (ex.InnerException.Message.ToUpper().Contains("REFERENCE CONSTRAINT"))
//                        throw new AppException("Unable to delete user record. The user information might have been linked to other records.");
//                    else
//                        throw new AppException("Unable to delete user record.");
//                }
//            }
//        }

//        // private helper methods
//        private static void CreatePasswordHash(string inPassword, out byte[] inPasswordHash, out byte[] inPasswordSalt)
//        {
//            if (inPassword == null) throw new ArgumentNullException("password");
//            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
//            //The password is hashed with a new random salt.
//            //https://crackstation.net/hashing-security.htm
//            using (var hmac = new System.Security.Cryptography.HMACSHA512())
//            {
//                inPasswordSalt = hmac.Key;
//                inPasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
//            }
//        }

//        private static bool VerifyPasswordHash(string inPassword, byte[] inStoredHash, byte[] inStoredSalt)
//        {
//            if (inPassword == null) throw new ArgumentNullException("password");
//            if (string.IsNullOrWhiteSpace(inPassword)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
//            if (inStoredHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
//            if (inStoredSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

//            using (var hmac = new System.Security.Cryptography.HMACSHA512(inStoredSalt))
//            {
//                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(inPassword));
//                for (int i = 0; i < computedHash.Length; i++)
//                {
//                    if (computedHash[i] != inStoredHash[i]) return false;
//                }
//            }

//            return true;
//        }

//    }
//}
