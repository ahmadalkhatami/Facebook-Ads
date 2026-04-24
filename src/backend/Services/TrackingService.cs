using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DecisionEngine.Core.Entities;

namespace DecisionEngine.Services
{
    public class TrackingService
    {
        private readonly HttpClient _httpClient;
        private readonly FacebookConfig _config;

        public TrackingService(HttpClient httpClient, FacebookConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v19.0/");
        }

        public async Task SendEventAsync(ConversionEvent evt)
        {
            var url = $"act_{_config.AdAccountId}/events?access_token={_config.AccessToken}";
            
            var payload = new
            {
                data = new[]
                {
                    new
                    {
                        event_name = evt.EventName,
                        event_time = evt.EventTime,
                        user_data = new
                        {
                            em = HashData(evt.Email),
                            ph = HashData(evt.Phone),
                            client_ip_address = evt.ClientIpAddress,
                            client_user_agent = evt.ClientUserAgent
                        },
                        custom_data = new
                        {
                            currency = evt.Currency,
                            value = evt.Value
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"[CAPI] Event {evt.EventName} sent. Response: {response.StatusCode}");
        }

        private string HashData(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data.Trim().ToLower()));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
