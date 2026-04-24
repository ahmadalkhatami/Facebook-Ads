using Microsoft.AspNetCore.Mvc;
using DecisionEngine.Infrastructure.Database;
using DecisionEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DecisionEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(AppDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var campaigns = await _context.Campaigns.ToListAsync();

            if (!campaigns.Any())
            {
                return Ok(new
                {
                    TotalSpend = 0m,
                    TotalRevenue = 0m,
                    AverageRoas = 0m,
                    AverageCtr = 0m,
                    TotalCampaigns = 0,
                    ActiveCampaigns = 0,
                    KilledCampaigns = 0
                });
            }

            var stats = new
            {
                TotalSpend = campaigns.Sum(c => c.Spend),
                TotalRevenue = campaigns.Sum(c => c.Revenue),
                AverageRoas = campaigns.Where(c => c.Roas > 0).Any()
                    ? campaigns.Where(c => c.Roas > 0).Average(c => c.Roas)
                    : 0m,
                AverageCtr = campaigns.Where(c => c.Ctr > 0).Any()
                    ? campaigns.Where(c => c.Ctr > 0).Average(c => c.Ctr)
                    : 0m,
                TotalCampaigns = campaigns.Count,
                ActiveCampaigns = campaigns.Count(c => c.Status == "ACTIVE"),
                KilledCampaigns = campaigns.Count(c => c.Status == "KILLED")
            };

            return Ok(stats);
        }

        [HttpGet("campaigns")]
        public async Task<IActionResult> GetCampaigns()
        {
            var campaigns = await _context.Campaigns
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.FbCampaignId,
                    c.Status,
                    c.Budget,
                    c.Spend,
                    c.Revenue,
                    c.Roas,
                    c.Ctr,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.Score)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Rating,
                    p.SoldCount,
                    p.Score,
                    p.Reason,
                    p.Status,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpGet("decisions")]
        public async Task<IActionResult> GetDecisionLogs([FromQuery] int limit = 50)
        {
            var logs = await _context.DecisionLogs
                .OrderByDescending(d => d.CreatedAt)
                .Take(limit)
                .Select(d => new
                {
                    d.Id,
                    d.CampaignId,
                    d.Action,
                    d.Reason,
                    d.Metadata,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpGet("metrics/daily")]
        public async Task<IActionResult> GetDailyMetrics([FromQuery] int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var metrics = await _context.Metrics
                .Where(m => m.RecordedAt >= since)
                .GroupBy(m => m.RecordedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSpend = g.Sum(m => m.Spend),
                    TotalRevenue = g.Sum(m => m.Revenue),
                    AverageRoas = g.Average(m => m.Roas),
                    AverageCtr = g.Average(m => m.Ctr)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            return Ok(metrics);
        }
    }
}
