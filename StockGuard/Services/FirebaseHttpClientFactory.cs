using Firebase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Firebase.Database;

namespace StockGuard.Services
{
    public class FirebaseHttpClientFactory : IHttpClientFactory
    {
        public IHttpClientProxy GetHttpClient(
            TimeSpan? timeout = null)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler);

            if (timeout.HasValue)
                client.Timeout = timeout.Value;

            return new FirebaseHttpClientProxy(client);
        }
    }
}