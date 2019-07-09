using BraintreeHttp;
using FYP.Data;
using FYP.Helpers;
using FYP.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Jpeg;
using Amazon.S3.Transfer;
using Amazon.S3;
using System.Net;
using System.Globalization;
using LazyCache;

namespace FYP.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAll();
        Task<IEnumerable<Product>> GetByPage(int pageNumber, int productsPerPage);
        Task<int> GetTotalNumberOfProducts();
        Task<Product> GetById(int id);
        Task<Product> Create(Product product);
        Task Update(Product productParam);
        Task Delete(int id);
    }

    public class ProductService : IProductService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;
        private readonly IS3Service _s3Service;
        private readonly IAppCache _cache;

        public ProductService(ApplicationDbContext context, 
            IOptions<AppSettings> appSettings,
            IS3Service s3Service,
            IAppCache appCache)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _s3Service = s3Service;
            _cache = appCache;
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            // returns full list of products including join with relevant tables
            // define a func to get the products but do not Execute() it
            Func<Task<IEnumerable<Product>>> productGetter = async () => await _context.Products
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(option => option.ProductImages)
                .ToListAsync();

            // get the results from the cache based on a unique key, or 
            // execute the func and cache the results
            return await _cache.GetOrAddAsync("AllProducts.Get", productGetter, DateTimeOffset.Now.AddHours(8));
        }

        public async Task<IEnumerable<Product>> GetByPage(int pageNumber, int productsPerPage)
        {
            Func<Task<IEnumerable<Product>>> productGetter = async () => await _context.Products
                .Skip((pageNumber - 1) * productsPerPage)
                .Take(productsPerPage)
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(o => o.ProductImages)
                .ToListAsync();

            return await _cache.GetOrAddAsync("ProductsByPage.Get", productGetter, DateTimeOffset.Now.AddHours(8));
        }

        public async Task<int> GetTotalNumberOfProducts()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<Product> GetById(int id)
        {
            // searches product, including join with relevant tables
            Func<Task<Product>> productGetter = async () => await _context.Products
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(option => option.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            return await _cache.GetOrAddAsync($"ProductById.Get.{id}", productGetter, DateTime.Now.AddHours(8));
        }

        public async Task<Product> Create(Product product)
        {
            // checks if another product with the same name exists already
            if (await _context.Products.AnyAsync(p => p.ProductName == product.ProductName))
                throw new AppException("Product name '" + product.ProductName + "' already exists in the database.");
            
            try
            {
                // ensure the prices are properly entered
                List<DiscountPrice> newPrices = new List<DiscountPrice>();
                foreach (DiscountPrice price in product.DiscountPrices)
                {
                    newPrices.Add(new DiscountPrice
                    {
                        EffectiveStartDate = DateTime.ParseExact(price.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        EffectiveEndDate = DateTime.ParseExact(price.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        DiscountValue = decimal.Parse(price.DiscountValue.ToString()),
                        IsPercentage = bool.Parse(price.IsPercentage.ToString())
                    });
                }

                // ensure the new options are properly entered
                List<Option> newOptions = new List<Option>();
                foreach (Option op in product.Options)
                {
                    List<ProductImage> newImages = new List<ProductImage>();
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ImageKey = img.ImageKey,
                            ImageUrl = img.ImageUrl
                        });
                    };
                    newOptions.Add(new Option
                    {
                        SKUNumber = op.SKUNumber,
                        OptionType = op.OptionType,
                        OptionValue = op.OptionValue,
                        CurrentQuantity = int.Parse(op.CurrentQuantity.ToString()),
                        MinimumQuantity = int.Parse(op.MinimumQuantity.ToString()),
                        ProductImages = newImages
                    });
                }


                // create new product object to be added
                Product newProduct = new Product()
                {
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Price = decimal.Parse(product.Price.ToString()),
                    ImageWidth = double.Parse(product.ImageWidth.ToString()),
                    ImageHeight = double.Parse(product.ImageHeight.ToString()),
                    EffectiveStartDate = DateTime.ParseExact(product.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                    EffectiveEndDate = DateTime.ParseExact(product.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                    CreatedAt = DateTime.Now,
                    CreatedById = product.CreatedById,
                    UpdatedAt = DateTime.Now,
                    UpdatedById = product.UpdatedById,
                    DiscountPrices = newPrices,
                    Options = newOptions,
                    CategoryId = 2
                };
                
                // add to database
                await _context.Products.AddAsync(newProduct);
                await _context.SaveChangesAsync();

                // returns product once done
                return newProduct;
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to create product record.", new { message = ex.Message });
            }
        }

        public async Task Update(Product productParam)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == productParam.ProductId)
                .Include(p => p.DiscountPrices)
                .Include(p => p.Options)
                .ThenInclude(p => p.ProductImages)
                .SingleOrDefaultAsync();

            // if product does not exist
            if (product == null)
                throw new AppException("Product not found.");

            // product exists, try to update
            try {
                // checks if another product with the same name exists already
                if (await _context.Products.CountAsync(p => p.ProductName == productParam.ProductName) > 1)
                {
                    throw new AppException("Product name '" + productParam.ProductName + "' already exists in the database.");
                }

                product.ProductName = productParam.ProductName;
                product.Description = productParam.Description;
                product.Price = decimal.Parse(productParam.Price.ToString());
                product.ImageWidth = double.Parse(productParam.ImageWidth.ToString());
                product.ImageHeight = double.Parse(productParam.ImageHeight.ToString());
                product.EffectiveStartDate = DateTime.ParseExact(productParam.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.EffectiveEndDate = DateTime.ParseExact(productParam.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.UpdatedAt = DateTime.Now;
                _context.Products.Update(product);

                // ensure the input prices are properly entered
                List<DiscountPrice> newPrices = new List<DiscountPrice>();
                foreach (DiscountPrice price in productParam.DiscountPrices)
                {
                    newPrices.Add(new DiscountPrice
                    {
                        DiscountPriceId = price.DiscountPriceId,
                        ProductId = price.ProductId,
                        EffectiveStartDate = DateTime.ParseExact(price.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        EffectiveEndDate = DateTime.ParseExact(price.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        DiscountValue = decimal.Parse(price.DiscountValue.ToString()),
                        IsPercentage = bool.Parse(price.IsPercentage.ToString())
                    });
                }
                
                // ensure the new options are properly entered
                List<Option> newOptions = new List<Option>();
                foreach (Option op in productParam.Options)
                {
                    List<ProductImage> newImages = new List<ProductImage>();
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ProductImageId = img.ProductImageId,
                            OptionId = img.OptionId,
                            ImageKey = img.ImageKey,
                            ImageUrl = img.ImageUrl
                        });
                    };
                    newOptions.Add(new Option
                    {
                        OptionId = op.OptionId,
                        ProductId = op.ProductId,
                        SKUNumber = op.SKUNumber,
                        OptionType = op.OptionType,
                        OptionValue = op.OptionValue,
                        CurrentQuantity = int.Parse(op.CurrentQuantity.ToString()),
                        MinimumQuantity = int.Parse(op.MinimumQuantity.ToString()),
                        ProductImages = newImages
                    });
                }

                List<string> imagesToDelete = new List<string>();

                // Delete children records if it is removed from the new product
                foreach (DiscountPrice childDP in product.DiscountPrices)
                {
                    if (!newPrices.Any(p => p.DiscountPriceId == childDP.DiscountPriceId))
                        _context.DiscountPrices.Remove(childDP);
                }

                foreach (Option childOP in product.Options)
                {
                    if (!newOptions.Any(o => o.OptionId == childOP.OptionId))
                    {
                        foreach (ProductImage childImg in childOP.ProductImages)
                        {
                            imagesToDelete.Add(childImg.ImageKey);
                            _context.ProductImages.Remove(childImg);
                        }
                        _context.Options.Remove(childOP);
                    }
                    else
                    {
                        foreach (ProductImage childImg in childOP.ProductImages)
                        {
                            if (!newOptions.Any(o => o.ProductImages.Any(i => i.ProductImageId == childImg.ProductImageId)))
                            {
                                imagesToDelete.Add(childImg.ImageKey);
                                _context.ProductImages.Remove(childImg);
                            }
                        }
                    }
                }

                // Update and insert new children records
                foreach (DiscountPrice dpModel in newPrices)
                {
                    DiscountPrice existingPrice = product.DiscountPrices
                        .Where(d => d.DiscountPriceId == dpModel.DiscountPriceId)
                        .SingleOrDefault();

                    if (existingPrice != null)
                    {
                        // update the price since it exists
                        _context.Entry(existingPrice).CurrentValues.SetValues(dpModel);
                    }
                    else
                    {
                        // does not exist, thus insert
                        product.DiscountPrices.Add(dpModel);
                    }
                }

                foreach (Option opModel in newOptions)
                {
                    Option existingOption = product.Options
                        .Where(o => o.OptionId == opModel.OptionId)
                        .SingleOrDefault();

                    if (existingOption != null)
                    {
                        // update the images first
                        foreach (ProductImage imgModel in opModel.ProductImages)
                        {
                            ProductImage existingImage = existingOption.ProductImages
                                .Where(i => i.ProductImageId == imgModel.ProductImageId)
                                .SingleOrDefault();

                            if (existingImage != null)
                            {
                                // if image exists, update
                                _context.Entry(existingImage).CurrentValues.SetValues(imgModel);
                            }
                        }
                        // now update the options to the db
                        _context.Entry(existingOption).CurrentValues.SetValues(opModel);
                    }
                    else
                    {
                        // update the product's options with the new ones
                        product.Options.Add(opModel);
                    }
                }

                // all good, can save changes
                await _context.SaveChangesAsync();

                // finally, delete images from s3
                if (imagesToDelete.Count > 0)
                    await _s3Service.DeleteProductImagesAsync(imagesToDelete);
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to update product record.", new { message = ex.Message });
            }
        }

        // technically can't delete products, only make them "expire"
        public async Task Delete(int id)
        {
            // find product to delete
            var product = await _context.Products.FindAsync(id);

            // if product exists, delete
            if (product != null)
            {
                try
                {
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException.Message.ToUpper().Contains("REFERENCE CONSTRAINT"))
                        throw new AppException("Unable to delete product record. The product information might have been linked to other records.");
                    else
                        throw new AppException("Unable to delete product record.");
                }
            }
        }
        
    }
}
