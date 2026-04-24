using DecisionEngine.Core.Entities;
using DecisionEngine.Core.Interfaces;
using DecisionEngine.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DecisionEngine.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;
        private readonly ILLMService _aiService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(AppDbContext context, ILLMService aiService, ILogger<ProductService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<List<Product>> GetHighScoredProductsAsync()
        {
            return await _context.Products.Where(p => p.Score >= 8).ToListAsync();
        }

        public async Task ScoreProductAsync(Product product)
        {
            _logger.LogInformation("Scoring product: {Name} (Price: {Price}, Rating: {Rating}, Sold: {Sold})",
                product.Name, product.Price, product.Rating, product.SoldCount);

            string prompt = $@"
You are an e-commerce expert specializing in Facebook Ads dropshipping.
Analyze this product for advertising potential:

Product Name: {product.Name}
Price: ${product.Price}
Rating: {product.Rating}/5
Units Sold: {product.SoldCount}

Score from 1-10 based on:
1. Viral potential (shareable, wow factor, emotional trigger)
2. Profit margin potential (price point vs typical ad costs)
3. Problem-solving capability (does it solve a real problem?)
4. Market demand (based on sold count and rating)
5. Creative potential (can we make compelling video ads?)

Return ONLY valid JSON:
{{
  ""score"": <number 1-10>,
  ""reason"": ""<brief explanation in 1-2 sentences>""
}}";

            string response = await _aiService.GenerateResponseAsync(prompt);

            try
            {
                // Clean response - sometimes AI wraps JSON in markdown
                response = response.Trim();
                if (response.StartsWith("```"))
                {
                    response = response.Split('\n').Skip(1).TakeWhile(l => !l.StartsWith("```")).Aggregate((a, b) => a + b);
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<ScoringResult>(response,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                {
                    product.Score = Math.Clamp(result.Score, 1, 10);
                    product.Reason = result.Reason;
                    product.Status = "SCORED";
                    _logger.LogInformation("Product {Name} scored: {Score}/10 - {Reason}",
                        product.Name, product.Score, product.Reason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response for product {Name}: {Response}", product.Name, response);
                product.Score = 0;
                product.Reason = "AI Scoring failed - will retry.";
                product.Status = "SCORE_FAILED";
            }

            await _context.SaveChangesAsync();
        }

        private class ScoringResult
        {
            public int Score { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
