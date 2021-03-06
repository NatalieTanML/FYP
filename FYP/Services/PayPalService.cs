﻿using BraintreeHttp;
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
    public interface IPayPalService
    {
        Task<Product> VerifyUserCart(UserProduct userProduct);
        Task<HttpResponse> CreatePaypalTransaction(List<UserProduct> sanitiseProducts);
        Task<HttpResponse> CapturePaypalTransaction(String orderId);
    }

    public class PayPalService : IPayPalService
    {
        private ApplicationDbContext _context;
        private readonly AppSettings _appSettings;

        public PayPalService(ApplicationDbContext context, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _appSettings = appSettings.Value;
        }

        public async Task<Product> VerifyUserCart(UserProduct userProduct)
        {
            var product = await _context.Products
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.ProductId == userProduct.ProductId);

            var option = product.Options
                .Where(o => o.OptionId == userProduct.OptionId)
                .FirstOrDefault();

            // check the stock first
            // the reason why i put < 3 is to allow the warehouse to have 3 items
            // as backup stock, in case printing errors happen and they have to consume
            // existing stock. update the quantity here as needed/required
            if (option.CurrentQuantity < 3 || userProduct.Quantity > option.CurrentQuantity)
            {
                throw new AppException("Insufficient stock quantity. Please contact us for more info.");
            }
            else
            {
                return product;
            }
        }

        public async Task<HttpResponse> CreatePaypalTransaction(List<UserProduct> userProducts)
        {
            List<Task<Product>> productTasks = new List<Task<Product>>();
            foreach (var userProduct in userProducts)
            {
                productTasks.Add(VerifyUserCart(userProduct));
            }
            var productResults = await Task.WhenAll<Product>(productTasks);

            decimal totalPrice = 0;
            List<UserProduct> userProductList = new List<UserProduct>();
            for (int i = 0; i < productResults.Length; i++)
            {
                userProductList.Add(new UserProduct
                {
                    ProductId = productResults[i].ProductId,
                    ProductName = productResults[i].ProductName,
                    Quantity = userProducts[i].Quantity,
                    Price = productResults[i].Price
                });
                totalPrice += userProducts[i].Quantity * productResults[i].Price;
            }

            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");
            request.RequestBody(BuildRequestBody(userProductList, totalPrice));
            //3. Call PayPal to set up a transaction
            var response = await PayPalClient.Client().Execute(request);

            var result = response.Result<PayPalCheckoutSdk.Orders.Order>();
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

        /*
          Method to generate sample create order body with CAPTURE intent

          @return OrderRequest with created order request
         */
        private static OrderRequest BuildRequestBody(List<UserProduct> userProductList, decimal totalPrice)
        {
            List<Item> lineItems = SetLineItems(userProductList);

            OrderRequest orderRequest = new OrderRequest()
            {
                Intent = "CAPTURE",

                ApplicationContext = new ApplicationContext
                {
                    BrandName = "KidzaniaSG",
                    LandingPage = "BILLING",
                    UserAction = "CONTINUE",
                    ShippingPreference = "NO_SHIPPING"
                },
                PurchaseUnits = new List<PurchaseUnitRequest>
                {

                    new PurchaseUnitRequest{
                    ReferenceId =  "PUHF",
                    Description = "Customisable Goods",
                    CustomId = "CUST-HighFashions",
                    SoftDescriptor = "HighFashions",
                    Amount = new AmountWithBreakdown
                    {
                        CurrencyCode = "SGD",
                        Value = totalPrice.ToString(),
                        Breakdown = new AmountBreakdown
                        {
                            ItemTotal = new Money
                            {
                                CurrencyCode = "SGD",
                                Value = totalPrice.ToString()
                            },
                        }
                    },
                    Items = lineItems
                }
            }
            };
            return orderRequest;
        }

        private static List<Item> SetLineItems(List<UserProduct> userProductList)
        {
            List<Item> lineItemList = new List<Item>();
            foreach (var userProduct in userProductList)
            {
                lineItemList.Add(new Item
                {
                    Name = userProduct.ProductName,
                    //Description = "Green XL",
                    //Sku = "sku01",
                    UnitAmount = new Money
                    {
                        CurrencyCode = "SGD",
                        Value = userProduct.Price.ToString()
                    },
                    Quantity = userProduct.Quantity.ToString(),
                    //Category = "PHYSICAL_GOODS"
                });
            }
            return lineItemList;
        }

        public async Task<HttpResponse> CapturePaypalTransaction(string orderId)
        {
            var request = new OrdersCaptureRequest(orderId);
            request.Prefer("return=representation");
            request.RequestBody(new OrderActionRequest());
            // 3. Call PayPal to capture an order
            var response = await PayPalClient.Client().Execute(request);
            // 4. Save the capture ID to your database. Implement logic
            // to save capture to your database for future reference.      
            var result = response.Result<PayPalCheckoutSdk.Orders.Order>();
            Console.WriteLine("Status: {0}", result.Status);
            Console.WriteLine("Order Id: {0}", result.Id);
            Console.WriteLine("Intent: {0}", result.Intent);
            Console.WriteLine("Links:");
            foreach (LinkDescription link in result.Links)
            {
                Console.WriteLine("\t{0}: {1}\tCall Type: {2}", link.Rel, link.Href, link.Method);
            }
            Console.WriteLine("Capture Ids: ");
            foreach (PurchaseUnit purchaseUnit in result.PurchaseUnits)
            {
                foreach (Capture capture in purchaseUnit.Payments.Captures)
                {
                    Console.WriteLine("\t {0}", capture.Id);
                }
            }
            AmountWithBreakdown amount = result.PurchaseUnits[0].Amount;
            Console.WriteLine("Buyer:");
            Console.WriteLine("\tEmail Address: {0}\n\tName: {1}", result.Payer.EmailAddress, result.Payer.Name.FullName);

            return response;
        }
    }
}
