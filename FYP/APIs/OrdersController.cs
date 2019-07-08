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
using Newtonsoft.Json.Linq;

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
                    emailString = order.EmailString,
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
                            i.OrderImageKey,
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
        [AllowAnonymous]
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
                    emailString = order.EmailString,
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
                            i.OrderImageKey,
                            i.OrderImageUrl,
                            i.OptionId,
                            options = (new
                            {
                                optionId = i.Option.OptionId,
                                optionType = i.Option.OptionType,
                                optionValue = i.Option.OptionValue,
                                skuNumber = i.Option.SKUNumber,
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
            try
            {
                // try add to database
                Order newOrder = await _orderService.Create(inOrder);
                return Ok(new
                {
                    createSuccess = true,
                    message = "Order created successfully!",
                    order = newOrder
                });
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create order record.", new { message = ex.Message });
            }
        }

        [HttpGet("getOrderStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderStatus()
        {
            var statuses = await _orderService.GetAllStatus();

            List<object> statusList = new List<object>();
            foreach (Status status in statuses)
            {
                statusList.Add(new {
                    statusId = status.StatusId,
                    statusName = status.StatusName
                });

            }
            return new JsonResult(statusList);
        }

        [HttpGet("track/{refNo}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderTracking(string refNo)
        {
            try
            {
                var trackingInfo = await _orderService.GetOrderTracking(refNo);
                return Ok(new
                {
                    message = "Order tracking received successfully!",
                    trackInfo = trackingInfo
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("guid")]
        [AllowAnonymous]
        public IActionResult GenerateGUID()
        {
            return Ok(new { guid = Guid.NewGuid().ToString("N").ToUpper() });
        }

        [HttpGet("getOrderStatus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderStatus()
        {
            var statuses = await _orderService.GetAllStatus();

            List<object> statusList = new List<object>();
            foreach (Status status in statuses)
            {
                statusList.Add(new
                {
                    statusId = status.StatusId,
                    statusName = status.StatusName
                });

            }

            return new JsonResult(statusList);
        }

        [HttpPut("status/{isSuccessful:bool}")]
        public async Task<IActionResult> UpdateStatuses([FromBody] List<int> orderIds, bool isSuccessful)
        {
            int updatedById = 4; // update to current user
            try
            {
                var updatedOrders = await _orderService.UpdateStatuses(orderIds, updatedById, isSuccessful);

                return Ok(new
                {
                    message = "Updated orders statuses successfully!",
                    orders = updatedOrders
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("deliveryman/{deliveryManId:int}")]
        public async Task<IActionResult> AssignDeliveryman([FromBody] List<int> orderIds, int deliveryManId)
        {
            
            int updatedById = 4; // update to current user
            try
            {

                await _orderService.AssignDeliveryman(orderIds, deliveryManId, updatedById);

                return Ok(new { message = "Updated order(s) deliveryman successfully!" });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("recipient")]
        public async Task<IActionResult> UpdateRecipient([FromBody] JObject data)
        {
            List<int> orderIds = data["orderIds"].ToObject<List<int>>();
            OrderRecipient recipient = data["recipient"].ToObject<OrderRecipient>();
            int updatedById = 4;
            try
            {
                await _orderService.UpdateRecipient(orderIds, recipient, updatedById);
                return Ok(new { message = "Updated order(s) recipient successfuly!" });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
