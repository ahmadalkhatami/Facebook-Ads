using DecisionEngine.Core.Entities;
using DecisionEngine.Infrastructure.FacebookApi;
using DecisionEngine.Infrastructure.Database;
using StackExchange.Redis;
using System.Text.Json;

namespace DecisionEngine.Services
{
    public class DecisionService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly FacebookApiClient _fbClient;
        private readonly TrafficService _trafficService;
        private readonly AppDbContext _context;
        private readonly ILogger<DecisionService> _logger;

        // Safety: Maximum daily spend cap per campaign (in currency units)
        private const decimal MAX_DAILY_SPEND = 500.00m;
        // Safety: Maximum budget scaling per action
        private const decimal MAX_SCALE_MULTIPLIER = 1.5m;

        public DecisionService(
            IConnectionMultiplexer redis,
            FacebookApiClient fbClient,
            TrafficService trafficService,
            AppDbContext context,
            ILogger<DecisionService> logger)
        {
            _redis = redis;
            _fbClient = fbClient;
            _trafficService = trafficService;
            _context = context;
            _logger = logger;
        }

        public async Task EvaluateCampaign(Campaign campaign)
        {
            _logger.LogInformation("Evaluating campaign: {Name} (ID: {FbId})", campaign.Name, campaign.FbCampaignId);

            // Fetch real metrics from Facebook
            try
            {
                var metricsJson = await _fbClient.GetCampaignMetricsAsync(campaign.FbCampaignId);
                var data = metricsJson.GetProperty("data")[0];

                campaign.Spend = decimal.Parse(data.GetProperty("spend").GetString() ?? "0");
                campaign.Ctr = decimal.Parse(data.GetProperty("inline_link_click_ctr").GetString() ?? "0");

                if (data.TryGetProperty("purchase_roas", out var roasArr))
                {
                    campaign.Roas = decimal.Parse(roasArr[0].GetProperty("value").GetString() ?? "0");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch metrics for {FbId}, using stored values.", campaign.FbCampaignId);
                // Continue with existing stored values
            }

            // SAFETY CHECK: Budget limiter
            if (campaign.Spend >= MAX_DAILY_SPEND)
            {
                _logger.LogWarning("🚨 Campaign {Name} hit safety spend limit (${Spend} >= ${Max}). Force pausing.",
                    campaign.Name, campaign.Spend, MAX_DAILY_SPEND);
                await KillCampaignAsync(campaign, "Safety: Daily spend limit exceeded");
                return;
            }

            // Logic: Spend > 2x Product Price and Sales == 0 -> Kill
            if (campaign.Spend > 2 * campaign.ProductPrice && campaign.Revenue == 0)
            {
                await KillCampaignAsync(campaign, "High spend, zero revenue");
                return;
            }

            // Logic: ROAS > 2 -> Scale
            if (campaign.Roas > 2)
            {
                await ScaleCampaignAsync(campaign, 1.2m);
            }

            // Logic: CTR < 1.5% -> Replace Creative
            if (campaign.Ctr < 1.5m)
            {
                await ReplaceCreativeAsync(campaign);
            }

            campaign.UpdatedAt = DateTime.UtcNow;
        }

        private async Task KillCampaignAsync(Campaign campaign, string reason)
        {
            _logger.LogInformation("[ACTION] Kill campaign {FbId} - Reason: {Reason}", campaign.FbCampaignId, reason);

            // Actually pause the campaign on Facebook
            bool success = await _trafficService.KillCampaignAsync(campaign);
            if (success)
            {
                campaign.Status = "KILLED";
                await LogDecision(campaign, "KILL", reason);
                await PublishAlert("KILL", campaign, reason);
            }
            else
            {
                _logger.LogError("Failed to kill campaign {FbId} on Facebook", campaign.FbCampaignId);
            }
        }

        private async Task ScaleCampaignAsync(Campaign campaign, decimal multiplier)
        {
            // Safety: Cap the multiplier
            multiplier = Math.Min(multiplier, MAX_SCALE_MULTIPLIER);
            decimal newBudget = campaign.Budget * multiplier;

            // Safety: Don't scale beyond max daily spend
            if (newBudget > MAX_DAILY_SPEND)
            {
                _logger.LogWarning("Scale would exceed safety limit. Capping budget to ${Max}", MAX_DAILY_SPEND);
                newBudget = MAX_DAILY_SPEND;
            }

            _logger.LogInformation("[ACTION] Scale campaign {FbId} by {Mult}x (${Old} -> ${New}) - Reason: ROAS is {Roas}",
                campaign.FbCampaignId, multiplier, campaign.Budget, newBudget, campaign.Roas);

            // Actually scale on Facebook
            await _trafficService.ScaleCampaignAsync(campaign, multiplier);
            campaign.Budget = newBudget;

            await LogDecision(campaign, "SCALE", $"ROAS is {campaign.Roas}", new { Multiplier = multiplier, NewBudget = newBudget });
            await PublishAlert("SCALE", campaign, $"ROAS is {campaign.Roas}", new { Multiplier = multiplier, NewBudget = newBudget });
        }

        private async Task ReplaceCreativeAsync(Campaign campaign)
        {
            _logger.LogInformation("[ACTION] Replace creative for {FbId} - Reason: CTR is {Ctr}%", campaign.FbCampaignId, campaign.Ctr);

            await LogDecision(campaign, "REPLACE_CREATIVE", $"CTR is {campaign.Ctr}%");
            await PublishAlert("REPLACE_CREATIVE", campaign, $"CTR is {campaign.Ctr}%");
        }

        private async Task LogDecision(Campaign campaign, string action, string reason, object? metadata = null)
        {
            try
            {
                var log = new DecisionLog
                {
                    CampaignId = campaign.Id,
                    Action = action,
                    Reason = reason,
                    Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DecisionLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log decision for campaign {Id}", campaign.Id);
            }
        }

        private async Task PublishAlert(string action, Campaign campaign, string reason, object? details = null)
        {
            try
            {
                var db = _redis.GetDatabase();
                var alert = new
                {
                    action,
                    campaign_name = campaign.Name,
                    fb_campaign_id = campaign.FbCampaignId,
                    reason,
                    details,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                await db.PublishAsync(RedisChannel.Literal("fb_ads_alerts"), JsonSerializer.Serialize(alert));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish alert to Redis");
            }
        }
    }
}
