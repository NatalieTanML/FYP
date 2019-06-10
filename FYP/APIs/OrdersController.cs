using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FYP.Helpers;
using FYP.Hubs;
using FYP.Models;
using FYP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orders = await _orderService.GetAll();
                //List<object> orderList = new List<object>();
                //foreach (Order order in orders)
                //{
                //    orderList.Add(new
                //    {
                //        orderId = order.OrderId,
                //        createdAt = order.CreatedAt,
                //        updatedAt = order.UpdatedAt,
                //        orderSubtotal = order.OrderSubtotal,
                //        orderTotal = order.OrderTotal,
                //        referenceNo = order.ReferenceNo,
                //        request = order.Request,
                //        email = order.Email,
                //        updatedBy = order.UpdatedBy.Name,
                //        deliveryType = order.DeliveryType,
                //        address = order.Address,
                //        status = order.Status,
                //        deliveryMan = order.DeliveryMan,
                //        orderRecipient = order.OrderRecipient,
                //        orderItems = order.OrderItems
                //    });
                //}
                return new JsonResult(orders);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var order = await _orderService.GetById(id);

                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody]Order inOrder)
        {
            inOrder.OrderId = id;
            inOrder.UpdatedById = 4;
            try
            {
                await _orderService.UpdateStatus(id, inOrder);
                return Ok(new { message = "Updated order status successfully!" });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
