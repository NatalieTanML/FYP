using BraintreeHttp;
using Microsoft.Extensions.Configuration;
using PayPalCheckoutSdk.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace FYP.Services
{
    public class PayPalClient
    {
        
        /**
               Set up PayPal environment with sandbox credentials.
               In production, use ProductionEnvironment.
            */
        public static PayPalEnvironment Environment()
        {
            string paypalClientId = System.Environment.GetEnvironmentVariable("PayPal:ClientID");
            string paypalClientSecret = System.Environment.GetEnvironmentVariable("PayPal:ClientSecret");
            return new SandboxEnvironment(paypalClientId, paypalClientSecret);
        }

        /**
            Returns PayPalHttpClient instance to invoke PayPal APIs.
         */
        public static HttpClient Client()
        {
            return new PayPalHttpClient(Environment());
        }

        public static HttpClient Client(string refreshToken)
        {
            return new PayPalHttpClient(Environment(), refreshToken);
        }

        /**
            Use this method to serialize Object to a JSON string.
        */
        public static String ObjectToJSONString(Object serializableObject)
        {
            MemoryStream memoryStream = new MemoryStream();
            var writer = JsonReaderWriterFactory.CreateJsonWriter(
                        memoryStream, Encoding.UTF8, true, true, "  ");
            DataContractJsonSerializer ser = new DataContractJsonSerializer(serializableObject.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            ser.WriteObject(writer, serializableObject);
            memoryStream.Position = 0;
            StreamReader sr = new StreamReader(memoryStream);
            return sr.ReadToEnd();
        }
    }

}
