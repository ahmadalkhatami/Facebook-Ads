using DecisionEngine.Core.Entities;
using DecisionEngine.Infrastructure.FacebookApi;

namespace DecisionEngine.Services
{
    public class TrafficService
    {
        private readonly FacebookApiClient _fbClient;
        private readonly ILogger<TrafficService> _logger;

        public TrafficService(FacebookApiClient fbClient, ILogger<TrafficService> logger)
        {
            _fbClient = fbClient;
            _logger = logger;
        }

        public async Task ScaleCampaignAsync(Campaign campaign, decimal multiplier)
        {
            decimal newBudget = campaign.Budget * multiplier;
            _logger.LogInformation("Scaling campaign {FbId}: ${Old} -> ${New}",
                campaign.FbCampaignId, campaign.Budget, newBudget);

            bool success = await _fbClient.UpdateBudgetAsync(campaign.FbCampaignId, newBudget);
            if (!success)
            {
                _logger.LogError("Failed to scale campaign {FbId} on Facebook", campaign.FbCampaignId);
            }
        }

        public async Task<bool> KillCampaignAsync(Campaign campaign)
        {
            _logger.LogInformation("Killing/Pausing campaign {FbId}", campaign.FbCampaignId);
            return await _fbClient.PauseCampaignAsync(campaign.FbCampaignId);
        }

        public async Task<string> LaunchCampaignAsync(Product product, string creativeId, decimal startingBudget = 50.00m)
        {
            string campaignName = $"SCALPING_{product.Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
            _logger.LogInformation("Launching campaign: {Name} with budget ${Budget}", campaignName, startingBudget);

            string campaignId = await _fbClient.CreateCampaignAsync(campaignName, startingBudget);

            if (string.IsNullOrEmpty(campaignId))
            {
                _logger.LogError("Failed to create campaign for product: {ProductName}", product.Name);
                return string.Empty;
            }

            _logger.LogInformation("Campaign created successfully: {CampaignId}", campaignId);
            return campaignId;
        }
    }
}
