using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

        [HttpGet]
        [AllowAnonymous]
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
                    description = product.Description,
                    imageWidth = product.ImageWidth,
                    imageHeight = product.ImageHeight,
                    effectiveStartDate = product.EffectiveStartDate,
                    effectiveEndDate = product.EffectiveEndDate,
                    updatedAt = product.UpdatedAt,
                    updatedById = product.UpdatedById,
                    updatedBy = product.UpdatedBy.Name,
                    categoryId = product.CategoryId,
                    categoryName = product.Category.CategoryName,
                    discountPrice = product.DiscountPrices
                        .Select(i => new
                        {
                            i.DiscountPriceId,
                            i.EffectiveStartDate,
                            i.EffectiveEndDate,
                            i.DiscountValue,
                            i.IsPercentage
                        }),
                    options = product.Options
                        .Select(i => new
                        {
                            i.OptionId,
                            i.OptionType,
                            i.OptionValue,
                            i.CurrentQuantity,
                            i.MinimumQuantity,
                            productImages = i.ProductImages
                                .Select(p => new {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl
                                })
                        })
                });
            }
            return new JsonResult(productList);
        }

        [AllowAnonymous]
        [HttpPost("productsByPage")]
        public async Task<IActionResult> GetProductsByPage([FromForm] IFormCollection inFormData)
        {
            try
            {
                var pageNumber = int.Parse(inFormData["currentPage"]);
                var pageSize = int.Parse(inFormData["productsPerPage"]);

                var products = await _productService.GetByPage(pageNumber, pageSize);
                var totalNumberOfProducts = await _productService.GetTotalNumberOfProducts();

                List<object> productList = new List<object>();
                foreach (Product product in products)
                {
                    productList.Add(new
                    {
                        productId = product.ProductId,
                        productName = product.ProductName,
                        price = product.Price,
                        description = product.Description,
                        imageWidth = product.ImageWidth,
                        imageHeight = product.ImageHeight,
                        discountPrice = product.DiscountPrices
                            .Select(i => new
                            {
                                i.DiscountValue,
                                i.IsPercentage
                            }),
                        productImages = product.ProductImages
                            .Select(i => new
                            {
                                i.ImageUrl
                            }),
                        options = product.Options
                            .Select(i => new
                            {
                                i.OptionId,
                                i.OptionType,
                                i.OptionValue,
                                i.CurrentQuantity,
                                i.MinimumQuantity
                            })
                    });
                }
                var response = new { productList, totalNumberOfProducts };
                return new JsonResult(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                    description = product.Description,
                    imageWidth = product.ImageWidth,
                    imageHeight = product.ImageHeight,
                    effectiveStartDate = product.EffectiveStartDate,
                    effectiveEndDate = product.EffectiveEndDate,
                    updatedAt = product.UpdatedAt,
                    updatedById = product.UpdatedById,
                    categoryId = product.CategoryId,
                    categoryName = product.Category.CategoryName,
                    discountPrice = product.DiscountPrices
                        .Select(i => new
                        {
                            i.DiscountPriceId,
                            i.EffectiveStartDate,
                            i.EffectiveEndDate,
                            i.DiscountValue,
                            i.IsPercentage
                        }),
                    options = product.Options
                        .Select(i => new
                        {
                            i.OptionId,
                            i.OptionType,
                            i.OptionValue,
                            i.CurrentQuantity,
                            i.MinimumQuantity,
                            productImages = i.ProductImages
                                .Select(p => new {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl
                                })
                        })
                });
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to get product record.", new { message = ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProduct([FromBody]Product inProduct)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            inProduct.CreatedById = currentUserId;
            inProduct.UpdatedById = currentUserId;

            try
            {
                // try add to database
                await _productService.Create(inProduct);
                return Ok(new
                {
                    createSuccess = true,
                    message = "Product created successfully!",
                    product = inProduct
                });
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create product record.", new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product inProduct)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            inProduct.ProductId = id;
            inProduct.UpdatedById = currentUserId;
            
            try
            {
                // save (excluding password update)
                await _productService.Update(inProduct);
                return Ok(new {
                    message = "Updated product details successfully!"
                });
            }
            catch (Exception ex)
            {
                // return error message 
                throw new AppException("Unable to update product record.", new { message = ex.Message });
            }
        }

        // technically can't delete products, only make them "expire"
        [AllowAnonymous]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.Delete(id);
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to delete product record.", new { message = ex.Message });
            }
        }
        
    }
}