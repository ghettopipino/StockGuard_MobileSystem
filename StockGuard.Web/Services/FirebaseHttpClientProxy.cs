using Firebase;
using Firebase.Database;

namespace StockGuard.Web.Services
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