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
        
        public ProductService(ApplicationDbContext context, 
            IOptions<AppSettings> appSettings,
            IS3Service s3Service)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _s3Service = s3Service;
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            // returns full list of products including join with relevant tables
            return await _context.Products
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(option => option.ProductImages)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetByPage(int pageNumber, int productsPerPage)
        {
            return await _context.Products
                .Skip((pageNumber - 1) * productsPerPage)
                .Take(productsPerPage)
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(o => o.ProductImages)
                .ToListAsync();
        }

        public async Task<int> GetTotalNumberOfProducts()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<Product> GetById(int id)
        {
            // searches product, including join with relevant tables
            return await _context.Products
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(option => option.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<Product> Create(Product product)
        {
            // checks if another product with the same name exists already
            if (await _context.Products.AnyAsync(p => p.ProductName == product.ProductName))
                throw new AppException("Product name '" + product.ProductName + "' already exists in the database.");
            
            try
            {
                // upload images to s3
                //await _s3Service.UploadImageAsync("https://20190507test1.s3-ap-southeast-1.amazonaws.com/image.jpg");
                
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

                // ensure the options are properly entered
                List<Option> newOptions = new List<Option>();
                List<ProductImage> newImages = new List<ProductImage>();
                foreach (Option op in product.Options)
                {
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ImageKey = "img.jpg",
                            ImageUrl = "url to be updated"
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
                    newImages.Clear();
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
            var product = await _context.Products.FindAsync(productParam.ProductId);

            // if product does not exist
            if (product == null)
                throw new AppException("Product not found.");

            // product exists, try to update
            try {
                // ensure the prices are properly entered
                List<DiscountPrice> newPrices = new List<DiscountPrice>();
                foreach (DiscountPrice price in productParam.DiscountPrices)
                {
                    newPrices.Add(new DiscountPrice
                    {
                        EffectiveStartDate = DateTime.ParseExact(price.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        EffectiveEndDate = DateTime.ParseExact(price.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        DiscountValue = decimal.Parse(price.DiscountValue.ToString()),
                        IsPercentage = bool.Parse(price.IsPercentage.ToString())
                    });
                }

                // ensure the options are properly entered
                List<Option> newOptions = new List<Option>();
                List<ProductImage> newImages = new List<ProductImage>();
                foreach (Option op in productParam.Options)
                {
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ImageKey = "img.jpg",
                            ImageUrl = "url to be updated"
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
                    newImages.Clear();
                }

                // checks if another product with the same name exists already
                if (await _context.Products.CountAsync(p => p.ProductName == productParam.ProductName) > 1)
                {
                    throw new AppException("Product name '" + productParam.ProductName + "' already exists in the database.");
                }

                // all good, can update product properties now
                product.ProductName = productParam.ProductName;
                product.Description = productParam.Description;
                product.Price = decimal.Parse(productParam.Price.ToString());
                product.ImageWidth = double.Parse(productParam.ImageWidth.ToString());
                product.ImageHeight = double.Parse(productParam.ImageHeight.ToString());
                product.EffectiveStartDate = DateTime.ParseExact(productParam.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.EffectiveEndDate = DateTime.ParseExact(productParam.EffectiveEndDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.UpdatedAt = DateTime.Now;
                product.DiscountPrices = newPrices;
                product.Options = newOptions;
                product.CategoryId = 2;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();
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
