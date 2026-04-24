using DecisionEngine.Core.Entities;
using DecisionEngine.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DecisionEngine.Services
{
    public class OrchestrationService
    {
        private readonly ProductService _productService;
        private readonly CreativeService _creativeService;
        private readonly TrafficService _trafficService;
        private readonly AppDbContext _context;
        private readonly ILogger<OrchestrationService> _logger;

        // Safety: Max campaigns to launch per cycle
        private const int MAX_LAUNCHES_PER_CYCLE = 3;
        // Default starting budget per campaign
        private const decimal DEFAULT_STARTING_BUDGET = 50_000m; // Rp 50,000

        public OrchestrationService(
            ProductService productService,
            CreativeService creativeService,
            TrafficService trafficService,
            AppDbContext context,
            ILogger<OrchestrationService> logger)
        {
            _productService = productService;
            _creativeService = creativeService;
            _trafficService = trafficService;
            _context = context;
            _logger = logger;
        }

        public async Task RunFullLoopAsync()
        {
            _logger.LogInformation("--- Starting Full Automation Loop ---");

            // 1. Process unscored products
            var unscoredProducts = await _context.Products
                .Where(p => p.Status == "SCRAPED" || p.Status == "SCORE_FAILED")
                .Take(10) // Process max 10 per cycle to control API costs
                .ToListAsync();

            _logger.LogInformation("Found {Count} unscored products to process", unscoredProducts.Count);

            foreach (var product in unscoredProducts)
            {
                try
                {
                    await _productService.ScoreProductAsync(product);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to score product {Name}. Will retry next cycle.", product.Name);
                }
            }

            // 2. Launch campaigns for high-scored products (that haven't been launched yet)
            var readyProducts = await _context.Products
                .Where(p => p.Status == "SCORED" && p.Score >= 8)
                .Take(MAX_LAUNCHES_PER_CYCLE)
                .ToListAsync();

            _logger.LogInformation("Found {Count} high-scored products ready for launch", readyProducts.Count);

            int launchedCount = 0;
            foreach (var product in readyProducts)
            {
                try
                {
                    _logger.LogInformation("🚀 Launching automation for product: {Name} (Score: {Score})",
                        product.Name, product.Score);

                    // Generate Creatives
                    string hook = await _creativeService.GenerateHookAsync(product);
                    string script = await _creativeService.GenerateVideoScriptAsync(product);

                    _logger.LogInformation("Creatives generated for {Name}", product.Name);

                    // Launch on Facebook
                    string campaignId = await _trafficService.LaunchCampaignAsync(
                        product, "pending_creative", DEFAULT_STARTING_BUDGET);

                    if (string.IsNullOrEmpty(campaignId))
                    {
                        _logger.LogWarning("Failed to launch campaign for {Name}. Skipping.", product.Name);
                        continue;
                    }

                    // Save campaign to DB
                    var campaign = new Campaign
                    {
                        Name = $"SCALPING_{product.Name}",
                        FbCampaignId = campaignId,
                        Budget = DEFAULT_STARTING_BUDGET,
                        ProductPrice = product.Price,
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Campaigns.Add(campaign);
                    product.Status = "LAUNCHED";
                    launchedCount++;

                    _logger.LogInformation("✅ Campaign launched: {CampaignId} for product {Name}",
                        campaignId, product.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error launching campaign for product: {Name}", product.Name);
                    // Continue with other products
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("--- Automation Loop Completed. Scored: {Scored}, Launched: {Launched} ---",
                unscoredProducts.Count, launchedCount);
        }
    }
}
