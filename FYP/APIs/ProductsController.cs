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
            try
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
                        productImages = product.ProductImages
                            .Select(i => new
                            {
                                i.ProductImageId,
                                i.ImageKey,
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
                return new JsonResult(productList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                    productImages = product.ProductImages
                        .Select(i => new
                        {
                            i.ProductImageId,
                            i.ImageKey,
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
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product inProduct)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            // create new product object to be added
            Product newProduct = new Product()
            {
                ProductName = inProduct.ProductName,
                Description = inProduct.Description,
                Price = decimal.Parse(inProduct.Price.ToString()),
                ImageWidth = double.Parse(inProduct.ImageWidth.ToString()),
                ImageHeight = double.Parse(inProduct.ImageHeight.ToString()),
                EffectiveStartDate = DateTime.ParseExact(inProduct.EffectiveStartDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                EffectiveEndDate = DateTime.ParseExact(inProduct.EffectiveEndDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedById = currentUserId,
                UpdatedById = currentUserId,
                DiscountPrices = inProduct.DiscountPrices,
                // product images need to invoke another method 
                // for image compression & upload to s3 first
                // return the url links after upload
                // ProductImages = inProduct.ProductImages
                //CategoryId = int.Parse(inFormData["category"])
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
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product inProduct)
        {
            // get current logged in user's id
            //int currentUserId = int.Parse(User.FindFirst("userid").Value);
            int currentUserId = 4;

            Product product = new Product()
            {
                ProductId = id,
                ProductName = inProduct.ProductName,
                Description = inProduct.Description,
                Price = decimal.Parse(inProduct.Price.ToString()),
                ImageWidth = double.Parse(inProduct.ImageWidth.ToString()),
                ImageHeight = double.Parse(inProduct.ImageHeight.ToString()),
                EffectiveStartDate = DateTime.ParseExact(inProduct.EffectiveStartDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                EffectiveEndDate = DateTime.ParseExact(inProduct.EffectiveEndDate.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                UpdatedAt = DateTime.Now,
                UpdatedById = currentUserId,
                DiscountPrices = inProduct.DiscountPrices,
                // product images need to invoke another method 
                // for image compression & upload to s3 first
                // return the url links after upload
                // ProductImages = inProduct.ProductImages
                //CategoryId = int.Parse(inFormData["category"])
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
    }
}