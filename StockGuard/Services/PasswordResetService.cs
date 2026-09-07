using System.Net.Http.Json;

namespace StockGuard.Services
{
    public class PasswordResetService
    {
        private readonly HttpClient _httpClient;

        // ── API ADDRESS ──────────────────────────────────────────────────────
        // StockGuard API hosted on Azure.
        // Works on Windows and Android without running the API locally.
        private const string BaseUrl =
            "https://stockguard-api-fzhsdtc0dvf5dpa0.southeastasia-01.azurewebsites.net";

        public PasswordResetService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        // ── SEND VERIFICATION CODE ──────────────────────────────────────────
        public async Task<bool> SendVerificationCodeAsync(
            string email,
            string code)
        {
            try
            {
                var request = new
                {
                    Email = email,
                    Code = code
                };

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/password-reset/send-code",
                        request);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Password reset email error: {ex.Message}");

                return false;
            }
        }
    }
}