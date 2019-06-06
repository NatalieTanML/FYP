﻿using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//1. Import the PayPal SDK client that was created in `Set up Server-Side SDK`.
using BraintreeHttp;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;

namespace FYP.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAll();
        Task<Product> GetById(int id);
        Task<Product> Create(Product product);
        Task Update(Product productParam);
        Task Delete(int id);
        Task<HttpResponse> GetPayPalOrder(String orderId);
        Task<IEnumerable<Product>> GetUserCart(List<int>productid);
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

        public async Task<HttpResponse> GetPayPalOrder(string orderId)
        {
            OrdersGetRequest request = new OrdersGetRequest(orderId);
            //3. Call PayPal to get the transaction
            var response = await PayPalClient.client().Execute(request);
            //4. Save the transaction in your database. Implement logic to save transaction to your database for future reference.
            var result = response.Result<PayPalCheckoutSdk.Orders.Order>();
            Console.WriteLine("Retrieved Order Status");
            Console.WriteLine("Status: {0}", result.Status);
            Console.WriteLine("Order Id: {0}", result.Id);
            Console.WriteLine("Intent: {0}", result.Intent);
            Console.WriteLine("Links:");
            foreach (LinkDescription link in result.Links)
            {
                Console.WriteLine("\t{0}: {1}\tCall Type: {2}", link.Rel, link.Href, link.Method);
            }
            AmountWithBreakdown amount = result.PurchaseUnits[0].Amount;
            Console.WriteLine("Total Amount: {0} {1}", amount.CurrencyCode, amount.Value);

            return response;
        }

        //public async Task<IEnumerable<Product>> getusercart(int[] productid)
        //{
        //    //https://stackoverflow.com/questions/5624614/get-a-list-of-elements-by-their-id-in-entity-framework
        //    //https://stackoverflow.com/questions/16824510/select-multiple-records-based-on-list-of-ids-with-linq

        //    // var idlist = new int[1, 2, 2, 2, 2]; // same user is selected 4 times
        //    //var userprofiles = _datacontext.userprofile.where(e => idlist.contains(e)).tolist();
        //    //var roles = db.roles.where(r => user.roles.contains(r.roleid));

        //    // returns full list of products based on productid including join with category table
        //    //return await _context.products.include(product => product.category).tolistasync();

        //    return await _context.Products.Where(p => productid.Contains(p.ProductId)).ToList();
        //}

        public async Task<IEnumerable<Product>> GetUserCart(List<int> productid)
        {
            return await _context.Products.Where(product => productid.Contains(product.ProductId)).ToListAsync();
        }
    }
}
