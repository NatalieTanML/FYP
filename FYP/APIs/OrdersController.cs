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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.APIs
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : Controller
    {
        private IOrderService _orderService;
        private IEmailService _emailService;
        //private IUserService _userService;
        private readonly AppSettings _appSettings;

        public OrdersController(IOrderService orderService,
            IEmailService emailService,
            IOptions<AppSettings> appSettings)
        {
            _orderService = orderService;
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }

        // gets all orders
        [HttpGet]
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
                                skuNumber = i.Option.SKUNumber,
                                attributes = i.Option.Attributes
                                    .Select(e => new
                                    {
                                        e.AttributeId,
                                        e.AttributeType,
                                        e.AttributeValue
                                    }),
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

        // gets one order by its id
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
                                attributes = i.Option.Attributes
                                    .Select(e => new
                                    {
                                        e.AttributeId,
                                        e.AttributeType,
                                        e.AttributeValue
                                    }),
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

        // gets multiple orders by their ids
        [HttpPost("multi")]
        public async Task<IActionResult> GetMultipleById([FromBody] List<int> orderIds)
        {
            var orders = await _orderService.GetMultipleById(orderIds);
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
                                skuNumber = i.Option.SKUNumber,
                                attributes = i.Option.Attributes
                                    .Select(e => new
                                    {
                                        e.AttributeId,
                                        e.AttributeType,
                                        e.AttributeValue
                                    }),
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

        // creates a new order
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody]Order inOrder)
        {
            try
            {
                // add to database
                Order newOrder = await _orderService.Create(inOrder);
                // generate receipt
                await _emailService.SendReceipt(newOrder);
                return Ok(new
                {
                    message = "Order created successfully!",
                    order = newOrder
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // gets an order's current status
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

        // generates a random unique guid for the cart
        [HttpPost("guid")]
        [AllowAnonymous]
        public IActionResult GenerateGUID()
        {
            return Ok(new { guid = Guid.NewGuid().ToString("N").ToUpper() });
        }

        // gets a list of order status types
        [HttpGet("status")]
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

        // gets a list of delivery types
        [HttpGet("deliverytypes")]
        public async Task<IActionResult> GetAllDeliveryTypes()
        {
            var deliveryTypes = await _orderService.GetAllDeliveryTypes();
            return new JsonResult(deliveryTypes);
        }
        
        // updates an order
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrder([FromBody] Order order)
        {
            int updatedById = int.Parse(User.FindFirst("userid").Value);
            try
            {
                await _orderService.UpdateOrder(order, updatedById);
                return Ok(new
                {
                    message = "Updated order successfully!"
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // update order(s) status
        [HttpPut("status/{isSuccessful:bool}")]
        public async Task<IActionResult> UpdateStatuses([FromBody] List<int> orderIds, bool isSuccessful)
        {
            int updatedById = int.Parse(User.FindFirst("userid").Value);
            try
            {
                var updatedOrders = await _orderService.UpdateStatuses(orderIds, updatedById, isSuccessful);
                return Ok(new
                {
                    message = "Updated orders' statuses successfully!",
                    orders = updatedOrders
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // updates an order to have a delivery man
        [HttpPut("deliveryman/{deliveryManId:int}")]
        public async Task<IActionResult> AssignDeliveryman([FromBody] List<int> orderIds, int deliveryManId)
        {
            int updatedById = int.Parse(User.FindFirst("userid").Value);
            try
            {
                await _orderService.AssignDeliveryman(orderIds, deliveryManId, updatedById);
                return Ok(new { message = "Updated orders' delivery man successfully!" });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // updates an order as received and with a recipient name + signature
        [HttpPut("recipient")]
        public async Task<IActionResult> UpdateRecipient([FromBody] JObject data)
        {
            List<int> orderIds = data["orderIds"].ToObject<List<int>>();
            OrderRecipient recipient = data["recipient"].ToObject<OrderRecipient>();
            int updatedById = int.Parse(User.FindFirst("userid").Value);
            try
            {
                var updatedOrders = await _orderService.UpdateRecipient(orderIds, recipient, updatedById);
                return Ok(new
                {
                    message = "Updated orders' recipient successfully!",
                    orders = updatedOrders
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
