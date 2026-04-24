using DecisionEngine.Core.Entities;
using DecisionEngine.Core.Interfaces;
using DecisionEngine.Infrastructure.Database;

namespace DecisionEngine.Services
{
    public class CreativeService
    {
        private readonly ILLMService _aiService;
        private readonly AppDbContext _context;
        private readonly ILogger<CreativeService> _logger;

        public CreativeService(ILLMService aiService, AppDbContext context, ILogger<CreativeService> logger)
        {
            _aiService = aiService;
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateHookAsync(Product product)
        {
            _logger.LogInformation("Generating hooks for product: {Name}", product.Name);

            string prompt = $@"
You are a high-converting Facebook ads copywriter for the Indonesian market.
Create 5 hooks for this product:
{product.Name} (Price: Rp{product.Price:N0})

Rules:
- Max 10 words each
- Pattern interrupt (unexpected opening)
- Emotional trigger
- In Bahasa Indonesia
- Focus on the problem this product solves

Output ONLY a JSON array of 5 strings, no explanation:
[""hook1"", ""hook2"", ""hook3"", ""hook4"", ""hook5""]";

            var response = await _aiService.GenerateResponseAsync(prompt);

            // Save creative to database
            try
            {
                var creative = new Creative
                {
                    ProductId = product.Id,
                    Hook = response,
                    Caption = "",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Creatives.Add(creative);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save creative for product {Name}", product.Name);
            }

            return response;
        }

        public async Task<string> GenerateVideoScriptAsync(Product product)
        {
            _logger.LogInformation("Generating video script for product: {Name}", product.Name);

            string prompt = $@"
Create a 15-second Facebook video ad script in Bahasa Indonesia.
Product: {product.Name}
Price: Rp{product.Price:N0}
Rating: {product.Rating}/5 ({product.SoldCount} terjual)

Structure:
- Hook (0-3 sec): Eye-catching opening
- Problem (3-7 sec): What problem does this solve?
- Solution (7-12 sec): Show the product solving it
- CTA (12-15 sec): Call to action with urgency

Return ONLY valid JSON:
{{
  ""hook"": ""<hook text>"",
  ""problem"": ""<problem scene description>"",
  ""solution"": ""<solution scene description>"",
  ""cta"": ""<call to action text>"",
  ""voiceover"": ""<full 15-second voiceover script>""
}}";

            return await _aiService.GenerateResponseAsync(prompt);
        }

        public async Task<string> GenerateCaptionAsync(Product product)
        {
            _logger.LogInformation("Generating ad caption for product: {Name}", product.Name);

            string prompt = $@"
Write a high-converting Facebook ad caption in Bahasa Indonesia.
Product: {product.Name}
Price: Rp{product.Price:N0}

Structure:
- Attention-grabbing first line
- 2-3 benefit bullets (use emojis)
- Social proof (mention rating/sales)
- Clear CTA with urgency

Max 150 words. Return the caption as plain text.";

            return await _aiService.GenerateResponseAsync(prompt);
        }
    }
}
