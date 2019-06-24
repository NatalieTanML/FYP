using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FYP.APIs
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private readonly AppSettings _appSettings;

        public UsersController(
            IUserService userService,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] User inUser)
        {
            //User newUser = new User()
            //{
            //    Name = inUser.Name,
            //    Email = inUser.Email,
            //    RoleId = inUser.RoleId,
            //    CreatedAt = DateTime.Now,
            //    CreatedById = 4,
            //    IsEnabled = true,
            //    ChangePassword = false,

            //};

            // set current user's id
            //inUser.CreatedById = int.Parse(User.FindFirst("userid").Value);
            inUser.CreatedById = 4;

            try
            {
                // save new user
                User newUserWithId = await _userService.Create(inUser);
                return Ok(new
                {
                    newUserWithId.UserId,
                    signUpStatus = true,
                    message = "User registered successfully"
                });
            }
            catch (Exception ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromForm] IFormCollection inFormData)
        {
            var user = await _userService.Authenticate(inFormData["username"], inFormData["password"]);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect." });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserId.ToString()),
                    new Claim("userid", user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.RoleName),
                    new Claim("isenabled", user.IsEnabled.ToString()),
                    new Claim("changepassword", user.ChangePassword.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info (w/o password) and token to store @ client side
            return Ok(new
            {
                user = new
                {
                    userId = user.UserId,
                    name = user.Name,
                    email = user.Email,
                    isEnabled = user.IsEnabled,
                    changePassword = user.ChangePassword
                },
                token = tokenString
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAll();
            List<object> userList = new List<object>();
            foreach (User user in users)
            {
                userList.Add(new
                {
                    userId = user.UserId,
                    roleName = user.Role.RoleName,
                    email = user.Email,
                    isEnabled = user.IsEnabled,
                    changePassword = user.ChangePassword
                });
            }
            return new JsonResult(userList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetById(id);
            return Ok(new
            {
                id = user.UserId,
                email = user.Email
            });
        }

        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            int currentUserId = int.Parse(User.FindFirst("userid").Value);
            var user = await _userService.GetById(currentUserId);

            return Ok(new
            {
                id = user.UserId,
                userRole = user.Role.RoleName.ToLower(),
                isEnabled = user.IsEnabled,
                changePassword = user.ChangePassword
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, IFormCollection inFormData)
        {
            User user = new User()
            {
                ChangePassword = true,
                UserId = id,
                Email = inFormData["username"]
                // update with disabled/not disabled
            };
            string password = inFormData["password"];
            try
            {
                // save (excluding password update)
                await _userService.Update(user, password);
                return Ok(new { message = "Completed user profile update." });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.Delete(id);
            return Ok(new { message = "User deleted successfully." });
        }
    }
}
