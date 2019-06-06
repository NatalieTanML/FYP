using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FYP.Helpers;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FYP.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private IOrderService _orderService;
        //private IUserService _userService;
        private readonly AppSettings _appSettings;

        public OrdersController(IOrderService orderService, IOptions<AppSettings> appSettings)
        {
            _orderService = orderService;
            _appSettings = appSettings.Value;
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    var orders = await _orderService.GetAll();
        //    List<object> orderList = new List<object>();
        //    foreach (Order order in orders)
        //    {
        //        orderList.Add(new
        //        {
        //            orderId = order.OrderId,
                    
        //        })
        //    }
        //}
    }
}
