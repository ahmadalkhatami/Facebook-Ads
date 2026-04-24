using System.Net.Http.Headers;
using System.Text.Json;
using DecisionEngine.Core.Entities;

namespace DecisionEngine.Infrastructure.FacebookApi
{
    public class FacebookApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly FacebookConfig _config;

        public FacebookApiClient(HttpClient httpClient, FacebookConfig config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v19.0/");
        }

        public async Task<bool> PauseCampaignAsync(string campaignId)
        {
            var url = $"{campaignId}?status=PAUSED&access_token={_config.AccessToken}";
            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateBudgetAsync(string campaignId, decimal newBudget)
        {
            // Note: Facebook budget is usually in cents or based on currency
            var url = $"{campaignId}?daily_budget={newBudget * 100}&access_token={_config.AccessToken}";
            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }

        public async Task<JsonElement> GetCampaignMetricsAsync(string campaignId)
        {
            var url = $"{campaignId}/insights?fields=spend,inline_link_click_ctr,purchase_roas&access_token={_config.AccessToken}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(content);
        }

        public async Task<string> CreateCampaignAsync(string name, decimal dailyBudget)
        {
            var url = $"act_{_config.AdAccountId}/campaigns?name={name}&objective=OUTCOME_SALES&status=PAUSED&daily_budget={dailyBudget * 100}&access_token={_config.AccessToken}";
            var response = await _httpClient.PostAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            return result.GetProperty("id").GetString() ?? string.Empty;
        }
    }
}
