using Firebase;
using Firebase.Database;

namespace StockGuard.Web.Services
{
    public class FirebaseHttpClientFactory : Firebase.IHttpClientFactory
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