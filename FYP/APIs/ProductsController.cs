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
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FYP.APIs
{
    [Authorize]
    [Route("api/[controller]")]
    public class ProductsController : Controller
    {
        private IProductService _productService;
        private readonly AppSettings _appSettings;

        public ProductsController(IProductService productService,
            IOptions<AppSettings> appSettings)
        {
            _productService = productService;
            _appSettings = appSettings.Value;
        }

        // gets all products
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
                    description = product.Description,
                    imageWidth = product.ImageWidth,
                    imageHeight = product.ImageHeight,
                    effectiveStartDate = product.EffectiveStartDate,
                    effectiveEndDate = product.EffectiveEndDate,
                    updatedAt = product.UpdatedAt,
                    updatedById = product.UpdatedById,
                    updatedBy = product.UpdatedBy?.Name,
                    categoryId = product.CategoryId,
                    categoryName = product.Category?.CategoryName,
                    discountPrice = product.DiscountPrices?
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
                            i.SKUNumber,
                            i.CurrentQuantity,
                            i.MinimumQuantity,
                            productImages = i.ProductImages
                                .Select(p => new
                                {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl,
                                    p.ImageSize
                                }),
                            attributes = i.Attributes
                                .Select(p => new
                                {
                                    p.AttributeId,
                                    p.AttributeType,
                                    p.AttributeValue
                                })
                        })
                });
            }
            return new JsonResult(productList);
        }

        // gets products by the page number for ecommerce
        [AllowAnonymous]
        [HttpPost("page")]
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
                    var effectiveDiscountPrice = _productService.RetrieveEffectiveDiscount(product);

                    productList.Add(new
                    {
                        productId = product.ProductId,
                        productName = product.ProductName,
                        price = product.Price,
                        description = product.Description,
                        imageWidth = product.ImageWidth,
                        imageHeight = product.ImageHeight,
                        discounts = effectiveDiscountPrice,
                        options = product.Options
                        .Select(i => new
                        {
                            i.OptionId,
                            i.CurrentQuantity,
                            i.MinimumQuantity,
                            productImages = i.ProductImages
                                .Select(p => new
                                {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl
                                }),
                            attributes = i.Attributes
                                .Select(p => new
                                {
                                    p.AttributeId,
                                    p.AttributeType,
                                    p.AttributeValue
                                })
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

        // gets a product and its full details for admin portal
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
                    effectiveEndDate = (DateTime?)product.EffectiveEndDate,
                    updatedAt = product.UpdatedAt,
                    updatedById = product.UpdatedById,
                    categoryId = (int?)product.CategoryId,
                    categoryName = product.Category?.CategoryName,
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
                            i.SKUNumber,
                            i.CurrentQuantity,
                            i.MinimumQuantity,
                            productImages = i.ProductImages
                                .Select(p => new
                                {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl,
                                    p.ImageSize
                                }),
                            attributes = i.Attributes
                                .Select(p => new
                                {
                                    p.AttributeId,
                                    p.AttributeType,
                                    p.AttributeValue
                                })
                        })
                });
            }
            catch (NullReferenceException)
            {
                return BadRequest(new { message = "Product does not exist." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // gets a product with its active discount (if any) for ecommerce
        [AllowAnonymous]
        [HttpGet("ecommerce/{id}")]
        public async Task<IActionResult> GetProductEcommerce(int id)
        {
            try
            {
                var product = await _productService.GetById(id);
                var effectiveDiscountPrice = _productService.RetrieveEffectiveDiscount(product);

                return Ok(new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    price = product.Price,
                    description = product.Description,
                    imageWidth = product.ImageWidth,
                    imageHeight = product.ImageHeight,
                    categoryId = product.CategoryId,
                    categoryName = product.Category?.CategoryName,
                    discounts = effectiveDiscountPrice,
                    options = product.Options
                        .Select(i => new
                        {
                            i.OptionId,
                            i.CurrentQuantity,
                            productImages = i.ProductImages
                                .Select(p => new
                                {
                                    p.ProductImageId,
                                    p.ImageKey,
                                    p.ImageUrl
                                }),
                            attributes = i.Attributes
                                .Select(p => new
                                {
                                    p.AttributeId,
                                    p.AttributeType,
                                    p.AttributeValue
                                })
                        })
                });
            }
            catch (NullReferenceException)
            {
                return BadRequest(new { message = "Product does not exist." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // creates a new product
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody]Product inProduct)
        {
            // get current logged in user's id
            int currentUserId = int.Parse(User.FindFirst("userid").Value);
            //int currentUserId = 4;

            inProduct.CreatedById = currentUserId;
            inProduct.UpdatedById = currentUserId;

            try
            {
                // try add to database
                Product newProduct = await _productService.Create(inProduct);
                return Ok(new
                {
                    createSuccess = true,
                    message = "Product created successfully!",
                    product = newProduct
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // updates a product's details
        [HttpPut]
        public async Task<IActionResult> UpdateProduct([FromBody] Product inProduct)
        {
            // get current logged in user's id
            int currentUserId = int.Parse(User.FindFirst("userid").Value);
            inProduct.UpdatedById = currentUserId;

            try
            {
                await _productService.Update(inProduct);
                return Ok(new
                {
                    message = "Updated product details successfully!"
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // updates a product's stock
        [HttpPut("stock/{id:int}/{amount:int}")]
        public async Task<IActionResult> UpdateStock(int id, int amount)
        {
            // id is for option id
            try
            {
                await _productService.UpdateStock(id, amount);
                return Ok(new
                {
                    message = "Updated stock successfully!"
                });
            }
            catch (Exception ex)
            {
                // return error message 
                return BadRequest(new { message = ex.Message });
            }
        }

        // technically can't delete products, only make them "expire"
        // use only if absolutely needed
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
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}