using Firebase;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class FirebaseHttpClientProxy : IHttpClientProxy
    {
        private readonly HttpClient _client;

        public FirebaseHttpClientProxy(HttpClient client)
        {
            _client = client;
        }

        public HttpClient GetHttpClient()
        {
            return _client;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
