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
        private readonly AppSettings _appSettings;

        public ProductsController(IProductService productService, IOptions<AppSettings> appSettings)
        {
            _productService = productService;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetOrder();
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
        [HttpGet("getproductbyid/{id}")]
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
