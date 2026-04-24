using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DecisionEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using DecisionEngine.Infrastructure.Database;
using DecisionEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DecisionEngine
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _services;

        // Intervals
        private static readonly TimeSpan DecisionInterval = TimeSpan.FromHours(1);
        private static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(5);

        public Worker(ILogger<Worker> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Decision Engine Worker started at {Time}", DateTimeOffset.Now);

            // Wait a bit for other services to start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("--- Running decision engine evaluation at: {Time} ---", DateTimeOffset.Now);

                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var decisionService = scope.ServiceProvider.GetRequiredService<DecisionService>();
                        var orchestrationService = scope.ServiceProvider.GetRequiredService<OrchestrationService>();

                        // 1. Run Orchestration (New Products -> AI Scoring -> Launch)
                        try
                        {
                            await orchestrationService.RunFullLoopAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in orchestration loop. Continuing to evaluate existing campaigns.");
                        }

                        // 2. Evaluate existing campaigns
                        var campaigns = await context.Campaigns
                            .Where(c => c.Status == "ACTIVE")
                            .ToListAsync(stoppingToken);

                        _logger.LogInformation("Found {Count} active campaigns to evaluate", campaigns.Count);

                        foreach (var campaign in campaigns)
                        {
                            try
                            {
                                await decisionService.EvaluateCampaign(campaign);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error evaluating campaign {CampaignName} ({FbId}). Skipping.",
                                    campaign.Name, campaign.FbCampaignId);
                                // Continue evaluating other campaigns
                            }
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }

                    _logger.LogInformation("--- Evaluation cycle completed. Next run in {Interval} ---", DecisionInterval);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "💥 Critical error in Worker main loop. Retrying in 5 minutes.");
                    await Task.Delay(HealthCheckInterval, stoppingToken);
                    continue;
                }

                // Run every hour as per spec
                await Task.Delay(DecisionInterval, stoppingToken);
            }

            _logger.LogInformation("Decision Engine Worker stopped.");
        }
    }
}
