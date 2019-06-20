using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FYP.APIs
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PayPalController : Controller
    {
        private IPayPalService _payPalService;
        private readonly OrdersController _ordersController;
        private readonly AppSettings _appSettings;

        public PayPalController(IPayPalService payPalService, 
            IOptions<AppSettings> appSettings, 
            OrdersController ordersController)
        {
            _payPalService = payPalService;
            _ordersController = ordersController;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("createPaypalTransaction")]
        public async Task<IActionResult> CreatePaypalTransaction([FromForm] List<UserProduct> userProducts)
        {
            try
            {
                var order = await _payPalService.CreatePaypalTransaction(userProducts);
              
                FieldInfo field = typeof(BraintreeHttp.HttpResponse).GetField("result",
                       BindingFlags.NonPublic |
                       BindingFlags.Instance);
                dynamic value = field.GetValue(order);
            
                return Ok(new
                {
                    orderId = value.Id
                });

            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("capturePaypalTransaction")]
        public async Task<IActionResult> CapturePaypalTransaction([FromForm] IFormCollection inFormData)
        {
            try
            {
                var orderId = inFormData["orderId"];
                var order = await _payPalService.CapturePaypalTransaction(orderId);

                FieldInfo field = typeof(BraintreeHttp.HttpResponse).GetField("result",
                     BindingFlags.NonPublic |
                     BindingFlags.Instance);
                dynamic value = field.GetValue(order);

                return Ok(new
                {
                    status = value.Status
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
