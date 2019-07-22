using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FYP.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private IEmailService _emailService;
        private readonly AppSettings _appSettings;

        public EmailController(
            IEmailService emailService,
            IOptions<AppSettings> appSettings)
        {
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }

        [HttpPost("stock")]
        public async Task<IActionResult> NotifyLowStock([FromBody] List<Option> options)
        {
            try
            {
                await _emailService.NotifyLowStock(options);
                return Ok(new
                {
                    message = "Notified all users of low stock."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("message")]
        public async Task<IActionResult> CreateMessage([FromBody]Enquiries enquiries)
        {
            try
            {
                await _emailService.CreateMessage(enquiries);
                return Ok(new
                {
                    message = "Created message successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}