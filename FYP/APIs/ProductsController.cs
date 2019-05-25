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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private IProductService _productService;
        //private IUserService _userService;
        private readonly AppSettings _appSettings;

        public ProductsController(IProductService productService, IOptions<AppSettings> appSettings)
        {
            _productService = productService;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAll();
            List<object> productList = new List<object>();
            foreach (Product product in products)
            {
                productList.Add(new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    price = product.Price,
                    categoryName = product.Category.CategoryName,
                    description = product.Description,
                    currentQuantity = product.CurrentQuantity,
                    minimumQuantity = product.MinimumQuantity,
                    updatedBy = product.UpdatedBy
                });
            }
            return new JsonResult(productList);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetById(id);
                return Ok(new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    price = product.Price,
                    categoryName = product.Category.CategoryName,
                    description = product.Description,
                    currentQuantity = product.CurrentQuantity,
                    minimumQuantity = product.MinimumQuantity,
                    updatedBy = product.UpdatedBy
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm] IFormCollection inFormData)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            // create new product object to be added
            Product newProduct = new Product()
            {
                ProductName = inFormData["productname"],
                CurrentQuantity = int.Parse(inFormData["currentquantity"]),
                MinimumQuantity = int.Parse(inFormData["minimumquantity"]),
                Description = inFormData["description"],
                Price = decimal.Parse(inFormData["price"]),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedBy = currentUserId,
                UpdatedBy = currentUserId,
                CategoryId = int.Parse(inFormData["category"])
            };

            try
            {
                // try add to database
                await _productService.Create(newProduct);
                return Ok(new
                {
                    createSuccess = true,
                    message = "Product created successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, IFormCollection inFormData)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            Product product = new Product()
            {
                ProductId = id,
                ProductName = inFormData["productName"],
                CurrentQuantity = int.Parse(inFormData["currentQuantity"]),
                Description = inFormData["description"],
                Price = decimal.Parse(inFormData["price"]),
                UpdatedBy = currentUserId
            };

            try
            {
                // save (excluding password update)
                await _productService.Update(product);
                return Ok(new { message = "Updated product details successfully!" });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productService.Delete(id);
            return Ok(new { message = "Product deleted successfully." });
        }

        [AllowAnonymous]
        [HttpPost("getOrder")]
        public async Task<IActionResult> GetPayPalOrder([FromForm] IFormCollection inFormData)
        {
            var orderId = inFormData["orderId"];
            var order = await _productService.GetPayPalOrder(orderId);
            return new JsonResult(order);
        }
    }
}
    }