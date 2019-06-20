﻿using System;
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
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            
            var orders = await _orderService.GetAll();
            List<object> orderList = new List<object>();
            foreach (Order order in orders)
            {
                orderList.Add(new
                {
                    orderId = order.OrderId,
                    createdAt = order.CreatedAt,
                    updatedAt = order.UpdatedAt,
                    orderSubtotal = order.OrderSubtotal,
                    orderTotal = order.OrderTotal,
                    referenceNo = order.ReferenceNo,
                    request = order.Request,
                    email = order.Email,
                    updatedById = order.UpdatedById,
                    updatedBy = order.UpdatedBy?.Name,
                    deliveryTypeId = order.DeliveryTypeId,
                    deliveryType = order.DeliveryType.DeliveryTypeName,
                    addressId = order.AddressId,
                    address = new
                    {
                        addressLine1 = order.Address?.AddressLine1,
                        addressLine2 = order.Address?.AddressLine2,
                        postalCode = order.Address?.PostalCode,
                        unitNo = order.Address?.UnitNo,
                        country = order.Address?.Country,
                        state = order.Address?.State,
                        hotelId = order.Address?.HotelId,
                        hotel = new
                        {
                            hotelName = order.Address?.Hotel?.HotelName,
                            hotelAddress = order.Address?.Hotel?.HotelAddress,
                            hotelPostalCode = order.Address?.Hotel?.HotelPostalCode
                        }
                    },
                    statusId = order.StatusId,
                    status = order.Status.StatusName,
                    deliveryManId = order.DeliveryManId,
                    deliveryMan = (new
                    {
                        name = order.DeliveryMan?.Name,
                        email = order.DeliveryMan?.Email
                    }),
                    orderRecipientId = order.OrderRecipientId,
                    orderRecipient = (new
                    {
                        receivedBy = order.OrderRecipient?.ReceivedBy,
                        receivedAt = order.OrderRecipient?.ReceivedAt,
                        recipientSignature = order.OrderRecipient?.RecipientSignature,
                    }),
                    orderItems = order.OrderItems
                        .Select(i => new
                        {
                            i.OrderItemId,
                            i.Quantity,
                            i.OrderImageUrl,
                            i.OptionId,
                            options = (new
                            {
                                optionId = i.Option.OptionId,
                                optionType = i.Option.OptionType,
                                optionValue = i.Option.OptionValue,
                                product = (new
                                {
                                    productId = i.Option.Product.ProductId,
                                    productName = i.Option.Product.ProductName,
                                    price = i.Option.Product.Price
                                })
                            })
                        })
                });
            }
            return new JsonResult(orderList);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var order = await _orderService.GetById(id);

                return Ok(new
                {
                    orderId = order.OrderId,
                    createdAt = order.CreatedAt,
                    updatedAt = order.UpdatedAt,
                    orderSubtotal = order.OrderSubtotal,
                    orderTotal = order.OrderTotal,
                    referenceNo = order.ReferenceNo,
                    request = order.Request,
                    email = order.Email,
                    updatedById = order.UpdatedById,
                    updatedBy = order.UpdatedBy?.Name,
                    deliveryTypeId = order.DeliveryTypeId,
                    deliveryType = order.DeliveryType.DeliveryTypeName,
                    addressId = order.AddressId,
                    address = new
                    {
                        addressLine1 = order.Address?.AddressLine1,
                        addressLine2 = order.Address?.AddressLine2,
                        postalCode = order.Address?.PostalCode,
                        unitNo = order.Address?.UnitNo,
                        country = order.Address?.Country,
                        state = order.Address?.State,
                        hotelId = order.Address?.HotelId,
                        hotel = new
                        {
                            hotelName = order.Address?.Hotel?.HotelName,
                            hotelAddress = order.Address?.Hotel?.HotelAddress,
                            hotelPostalCode = order.Address?.Hotel?.HotelPostalCode
                        }
                    },
                    statusId = order.StatusId,
                    status = order.Status.StatusName,
                    deliveryManId = order.DeliveryManId,
                    deliveryMan = (new
                    {
                        name = order.DeliveryMan?.Name,
                        email = order.DeliveryMan?.Email
                    }),
                    orderRecipientId = order.OrderRecipientId,
                    orderRecipient = (new
                    {
                        receivedBy = order.OrderRecipient?.ReceivedBy,
                        receivedAt = order.OrderRecipient?.ReceivedAt,
                        recipientSignature = order.OrderRecipient?.RecipientSignature,
                    }),
                    orderItems = order.OrderItems
                        .Select(i => new
                        {
                            i.OrderItemId,
                            i.Quantity,
                            i.OrderImageUrl,
                            i.OptionId,
                            options = (new
                            {
                                optionId = i.Option.OptionId,
                                optionType = i.Option.OptionType,
                                optionValue = i.Option.OptionValue,
                                product = (new
                                {
                                    productId = i.Option.Product.ProductId,
                                    productName = i.Option.Product.ProductName,
                                    price = i.Option.Product.Price
                                })
                            })
                        })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody]Order inOrder)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            //int currentUserId = 4;

            try
            {
                // try add to database
                await _orderService.Create(inOrder);
                return Ok(new
                {
                    createSuccess = true,
                    message = "Order created successfully!",
                    order = inOrder
                });
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create order record.", new { message = ex.Message });
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
