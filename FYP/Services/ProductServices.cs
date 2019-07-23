using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
        Task UpdateStock(int id, int stockUpdate);
        Task Delete(int id);
        List<object> RetrieveEffectiveDiscount(Product product);
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
            return await _context.Products
                .Include(product => product.Category)
                .Include(product => product.DiscountPrices)
                .Include(product => product.Options)
                .ThenInclude(option => option.ProductImages)
                .Include(product => product.Options)
                .ThenInclude(o => o.Attributes)
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
                .Include(product => product.Options)
                .ThenInclude(o => o.Attributes)
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
                .Include(product => product.Options)
                .ThenInclude(o => o.Attributes)
                .FirstOrDefaultAsync(p => p.ProductId == id);
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
                        EffectiveEndDate = string.IsNullOrWhiteSpace(price.EffectiveEndDate.ToString()) ? (DateTime?)null : DateTime.ParseExact(price.EffectiveEndDate?.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                        DiscountValue = decimal.Parse(price.DiscountValue.ToString()),
                        IsPercentage = bool.Parse(price.IsPercentage.ToString())
                    });
                }

                // ensure the new options are properly entered
                List<Option> newOptions = new List<Option>();
                foreach (Option op in product.Options)
                {
                    // add the new images
                    List<ProductImage> newImages = new List<ProductImage>();
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ImageKey = img.ImageKey,
                            ImageUrl = img.ImageUrl,
                            ImageSize = img.ImageSize
                        });
                    };
                    // add the attributes
                    List<Models.Attribute> newAttributes = new List<Models.Attribute>();
                    foreach (Models.Attribute atr in op.Attributes)
                    {
                        newAttributes.Add(new Models.Attribute
                        {
                            AttributeType = atr.AttributeType,
                            AttributeValue = atr.AttributeValue
                        });
                    };
                    newOptions.Add(new Option
                    {
                        OptionId = op.OptionId,
                        ProductId = op.ProductId,
                        SKUNumber = op.SKUNumber,
                        CurrentQuantity = int.Parse(op.CurrentQuantity.ToString()),
                        MinimumQuantity = int.Parse(op.MinimumQuantity.ToString()),
                        ProductImages = newImages,
                        Attributes = newAttributes
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
                    EffectiveEndDate = string.IsNullOrWhiteSpace(product.EffectiveEndDate.ToString()) ? (DateTime?)null : DateTime.ParseExact(product.EffectiveEndDate?.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                    CreatedAt = DateTime.Now,
                    CreatedById = product.CreatedById,
                    UpdatedAt = DateTime.Now,
                    UpdatedById = product.UpdatedById,
                    DiscountPrices = newPrices,
                    Options = newOptions,
                    CategoryId = product.CategoryId.Equals(0) ? null : product.CategoryId
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
                .Include(p => p.Options)
                .ThenInclude(p => p.Attributes)
                .SingleOrDefaultAsync();

            // if product does not exist
            if (product == null)
                throw new AppException("Product not found.");

            // product exists, try to update
            try
            {
                // checks if another product with the same name exists already
                if (await _context.Products
                    .Where(p => p.ProductId != productParam.ProductId)
                    .AnyAsync(p => p.ProductName == productParam.ProductName))
                {
                    throw new AppException("Product name '" + productParam.ProductName + "' already exists in the database.");
                }

                product.ProductName = productParam.ProductName;
                product.Description = productParam.Description;
                product.Price = decimal.Parse(productParam.Price.ToString());
                product.ImageWidth = double.Parse(productParam.ImageWidth.ToString());
                product.ImageHeight = double.Parse(productParam.ImageHeight.ToString());
                product.EffectiveStartDate = DateTime.ParseExact(productParam.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.EffectiveEndDate = string.IsNullOrWhiteSpace(productParam.EffectiveEndDate.ToString()) ? (DateTime?)null : DateTime.ParseExact(productParam.EffectiveEndDate?.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                product.UpdatedAt = DateTime.Now;
                product.UpdatedById = productParam.UpdatedById;
                _context.Products.Update(product);

                // ensure the input prices are properly entered
                List<DiscountPrice> newPrices = new List<DiscountPrice>();
                if (productParam.DiscountPrices != null)
                {
                    foreach (DiscountPrice price in productParam.DiscountPrices)
                    {
                        newPrices.Add(new DiscountPrice
                        {
                            DiscountPriceId = price.DiscountPriceId,
                            ProductId = price.ProductId,
                            EffectiveStartDate = DateTime.ParseExact(price.EffectiveStartDate.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                            EffectiveEndDate = string.IsNullOrWhiteSpace(price.EffectiveEndDate.ToString()) ? (DateTime?)null : DateTime.ParseExact(price.EffectiveEndDate?.ToString(), "d/M/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture),
                            DiscountValue = decimal.Parse(price.DiscountValue.ToString()),
                            IsPercentage = bool.Parse(price.IsPercentage.ToString())
                        });
                    }
                }

                // ensure the new options are properly entered
                List<Option> newOptions = new List<Option>();
                foreach (Option op in productParam.Options)
                {
                    // add the new images
                    List<ProductImage> newImages = new List<ProductImage>();
                    foreach (ProductImage img in op.ProductImages)
                    {
                        newImages.Add(new ProductImage
                        {
                            ProductImageId = img.ProductImageId,
                            OptionId = img.OptionId,
                            ImageKey = img.ImageKey,
                            ImageUrl = img.ImageUrl,
                            ImageSize = img.ImageSize
                        });
                    };
                    // add the attributes
                    List<Models.Attribute> newAttributes = new List<Models.Attribute>();
                    foreach (Models.Attribute atr in op.Attributes)
                    {
                        newAttributes.Add(new Models.Attribute
                        {
                            AttributeId = atr.AttributeId,
                            OptionId = atr.OptionId,
                            AttributeType = atr.AttributeType,
                            AttributeValue = atr.AttributeValue
                        });
                    };
                    newOptions.Add(new Option
                    {
                        OptionId = op.OptionId,
                        ProductId = op.ProductId,
                        SKUNumber = op.SKUNumber,
                        CurrentQuantity = int.Parse(op.CurrentQuantity.ToString()),
                        MinimumQuantity = int.Parse(op.MinimumQuantity.ToString()),
                        ProductImages = newImages,
                        Attributes = newAttributes
                    });
                }

                // Delete children records if it is removed from the new product
                if (product.DiscountPrices != null)
                {
                    foreach (DiscountPrice childDP in product.DiscountPrices)
                    {
                        if (!newPrices.Any(p => p.DiscountPriceId == childDP.DiscountPriceId))
                            _context.DiscountPrices.Remove(childDP);
                    }
                }
                foreach (Option childOP in product.Options)
                {
                    // if the option is deleted altogether
                    if (!newOptions.Any(o => o.OptionId == childOP.OptionId))
                    {
                        foreach (ProductImage childImg in childOP.ProductImages)
                            _context.ProductImages.Remove(childImg);
                        foreach (Models.Attribute childAtr in childOP.Attributes)
                            _context.Attributes.Remove(childAtr);

                        _context.Options.Remove(childOP);
                    }
                    else
                    {
                        // option is not deleted, but child records under it are
                        foreach (ProductImage childImg in childOP.ProductImages)
                        {
                            if (!newOptions.Any(o => o.ProductImages.Any(i => i.ProductImageId == childImg.ProductImageId)))
                                _context.ProductImages.Remove(childImg);
                        }
                        foreach (Models.Attribute childAtr in childOP.Attributes)
                        {
                            if (!newOptions.Any(o => o.Attributes.Any(i => i.AttributeId == childAtr.AttributeId)))
                                _context.Attributes.Remove(childAtr);
                        }
                    }
                }

                // Update and insert new children records
                if (newPrices != null)
                {
                    foreach (DiscountPrice dpModel in newPrices)
                    {
                        DiscountPrice existingPrice = product.DiscountPrices
                            .Where(d => d.DiscountPriceId == dpModel.DiscountPriceId)
                            .Where(d => d.DiscountPriceId != 0)
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
                }

                foreach (Option opModel in newOptions)
                {
                    Option existingOption = product.Options
                        .Where(o => o.OptionId == opModel.OptionId)
                        .Where(o => o.OptionId != 0)
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
                            else
                            {
                                // does not exist, thus insert
                                existingOption.ProductImages.Add(imgModel);
                            }
                        }
                        // update attributes next
                        foreach (Models.Attribute atrModel in opModel.Attributes)
                        {
                            Models.Attribute existingAttribute = existingOption.Attributes
                                .Where(i => i.AttributeId == atrModel.AttributeId)
                                .SingleOrDefault();

                            if (existingAttribute != null)
                            {
                                // if attribute exists, update
                                _context.Entry(existingAttribute).CurrentValues.SetValues(atrModel);
                            }
                            else
                            {
                                // does not exist, thus insert
                                existingOption.Attributes.Add(atrModel);
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
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to update product record.", new { message = ex.Message });
            }
        }

        public async Task UpdateStock(int id, int stockUpdate)
        {
            // find option to update
            var option = await _context.Options.FindAsync(id);

            // if product does not exist
            if (option == null)
                throw new AppException("Option not found.");

            try
            {
                // update the stock
                // stockUpdate should be the amount of stock added/removed
                // e.g. current qty = 100, stockUpdate = 30 
                // 100 + 30 = 130 (new stock amt, add 30)
                // e.g.2 curr qty = 100, stockUpdate = -10
                // 100 + -10 = 90 (new stock amt, minus 10)
                option.CurrentQuantity += stockUpdate;
                _context.Options.Update(option);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new AppException("Unable to update option quantity.", new { message = ex.Message });
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

        public List<object> RetrieveEffectiveDiscount(Product product)
        {
            DateTime todayDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            List<object> effectiveDiscountPrice = new List<object>();
            var basePrice = product.Price;

            // if discount is percentage, add another column for the discount value
            foreach (var productDiscount in product.DiscountPrices)
            {
                // Check if today's date is within discount start date and end date
                // if there is no end date, check to see if discount is currently happening
                if ((todayDate >= productDiscount.EffectiveStartDate &&
                    todayDate < productDiscount.EffectiveEndDate) ||
                    (todayDate >= productDiscount.EffectiveStartDate &&
                    productDiscount.EffectiveEndDate == null))
                {
                    if (productDiscount.IsPercentage)
                    {
                        // Calculate the discounted price based on discount percentage
                        var discountPrice =
                            Math.Round(basePrice - (basePrice * (productDiscount.DiscountValue) / 100), 2);

                        effectiveDiscountPrice.Add(new
                        {
                            productDiscount.DiscountPriceId,
                            productDiscount.DiscountValue,
                            productDiscount.IsPercentage,
                            discountPrice
                        });
                    }
                    else
                    {
                        // Calculate the discount percentage based on the discount price
                        var discountValue = basePrice - productDiscount.DiscountValue;
                        var discountPercentage =
                            Math.Ceiling((basePrice - discountValue) / basePrice * 100);



                        effectiveDiscountPrice.Add(new
                        {
                            productDiscount.DiscountPriceId,
                            discountValue,
                            productDiscount.IsPercentage,
                            discountPercentage
                        });
                    }
                }
            }
            return effectiveDiscountPrice;
        }

    }
}
