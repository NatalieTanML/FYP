using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PayPalCheckoutSdk.Core;
using BraintreeHttp;

using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;

namespace FYP
{
    public class PayPalClient
    {
        /**
           Set up PayPal environment with sandbox credentials.
           In production, use ProductionEnvironment.
        */
        public static PayPalEnvironment environment()
        {
            return new SandboxEnvironment("ARMvb4xeFIpb2clROUsEC6qMzbAyY8fMA5JpJhx6Kz-SuzGny1SxY4otfBp93WKA7AnK6X7KxWUEIe9w", "EMy9DVggFxOQGEhNbEaeF45n-sPK6VXH6Xwvg8Al94ToSmOlXXagZ3_o_VkZVwufBYmg33hIGRs_9zun");
        }

        /**
            Returns PayPalHttpClient instance to invoke PayPal APIs.
         */
        public static HttpClient client()
        {
            return new PayPalHttpClient(environment());
        }

        public static HttpClient client(string refreshToken)
        {
            return new PayPalHttpClient(environment(), refreshToken);
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
