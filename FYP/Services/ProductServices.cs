using BraintreeHttp;
using FYP.Data;
using FYP.Helpers;
using FYP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FYP.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetOrder();
        Task<Product> GetById(int id);
        Task<HttpResponse> GetPayPalOrder (String orderId);
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

        public async Task<IEnumerable<Product>> GetOrder()
        {
            return await _context.Products.Include(product => product.Category).ToListAsync();
        }

        public async Task<Product> GetById(int id)
        {
            return await _context.Products.Include(product => product.Category).FirstOrDefaultAsync(p => p.ProductId == id);
        }

        public async Task<HttpResponse> GetPayPalOrder(string orderId)
        {
            OrdersGetRequest request = new OrdersGetRequest(orderId);
            //3. Call PayPal to get the transaction
            var response = await PayPalClient.client().Execute(request);
            //4. Save the transaction in your database. Implement logic to save transaction to your database for future reference.
            var result = response.Result<Order>();
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
    }
}
