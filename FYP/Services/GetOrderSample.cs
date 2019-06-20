using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayPalCheckoutSdk.Orders;
using BraintreeHttp;
using System.Diagnostics;

namespace FYP.Services
{
    public class GetOrderSample
    {
        public async static Task<HttpResponse> GetOrder(string orderId, bool debug = false)
        {
            OrdersGetRequest request = new OrdersGetRequest(orderId);



            //request.Headers.Add("prefer", "return=representation");
            // request.Headers.Add("PayPal-Partner-Attribution-Id", "PARTNER_ID_ASSIGNED_BY_YOUR_PARTNER_MANAGER");


            //3. Call PayPal to get the transaction
            var response = await PayPalClient.Client().Execute(request);
            //4. Save the transaction in your database. Implement logic to save transaction to your database for future reference.
            var result = response.Result<Order>();
            Debug.WriteLine("Retrieved Order Status");
            Debug.WriteLine("Status:" + result.Status);
            Debug.WriteLine("Order Id:" + result.Id);
            Debug.WriteLine("Intent:" + result.Intent);
            Debug.WriteLine("Links:");
            foreach (LinkDescription link in result.Links)
            {
                Debug.WriteLine("\t{0}: {1}\tCall Type: {2}", link.Rel, link.Href, link.Method);
            }
            AmountWithBreakdown amount = result.PurchaseUnits[0].Amount;
            Debug.WriteLine("Total Amount: {0} {1}", amount.CurrencyCode, amount.Value);

            return response;
        }
    }
}
