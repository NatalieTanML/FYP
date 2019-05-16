using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAll();
        Task<Product> GetById(int id);
        Task<Product> Create(Product product);
        Task Update(Product productParam);
        Task Delete(int id);
    }

    public class ProductService : IProductService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;

        public ProductService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<Product>> GetAll()
        {
            // returns full list of products including join with category table
            return await _context.Products.Include(product => product.Category).ToListAsync();
        }

        public async Task<Product> GetById(int id)
        {
            // searches product, including join with category
            return await _context.Products.Include(product => product.Category).FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<Product> Create(Product product)
        {
            // checks if another product with the same name exists already
            if (await _context.Products.AnyAsync(p => p.ProductName == product.ProductName))
                throw new AppException("Product name '" + product.ProductName + "' already exists in the database");

            // add to database
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // returns product once done
            return product;
        }

        public async Task Update(Product productParam)
        {
            var product = await _context.Products.FindAsync(productParam.ProductId);

            // if product does not exist
            if (product == null)
                throw new AppException("Product not found.");

            // update product properties
            product.ProductName = productParam.ProductName;
            product.CurrentQuantity = productParam.CurrentQuantity;
            product.Description = productParam.Description;
            product.Price = productParam.Price;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = productParam.UpdatedBy;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
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
