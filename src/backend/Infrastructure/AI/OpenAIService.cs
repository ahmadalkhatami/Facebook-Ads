using DecisionEngine.Core.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DecisionEngine.Infrastructure.AI
{
    public class OpenAIService : ILLMService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private const string MODEL = "gpt-4o-mini"; // Cost-effective, fast
        private const int MAX_RETRIES = 3;

        public OpenAIService(string apiKey, IHttpClientFactory httpClientFactory, ILogger<OpenAIService> logger)
        {
            _apiKey = apiKey;
            _httpClient = httpClientFactory.CreateClient("OpenAI");
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _logger = logger;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "MISSING_KEY" || _apiKey.StartsWith("your_"))
            {
                _logger.LogWarning("OpenAI API key not configured. Returning mock response.");
                return GetMockResponse(prompt);
            }

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    var request = new OpenAIRequest
                    {
                        Model = MODEL,
                        Messages = new[]
                        {
                            new Message { Role = "system", Content = "You are a helpful AI assistant. Always respond in valid JSON format when asked." },
                            new Message { Role = "user", Content = prompt }
                        },
                        Temperature = 0.7,
                        MaxTokens = 1000
                    };

                    var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("chat/completions", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning("OpenAI API error (attempt {Attempt}): {Status} - {Body}",
                            attempt, response.StatusCode, errorBody);

                        if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                        {
                            // Rate limited or server error — retry with backoff
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                            continue;
                        }

                        throw new Exception($"OpenAI API error: {response.StatusCode} - {errorBody}");
                    }

                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenAIResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    var aiContent = result?.Choices?.FirstOrDefault()?.Message?.Content;

                    if (string.IsNullOrEmpty(aiContent))
                    {
                        _logger.LogWarning("Empty response from OpenAI on attempt {Attempt}", attempt);
                        continue;
                    }

                    _logger.LogInformation("[AI] Generated response ({Tokens} tokens used)",
                        result?.Usage?.TotalTokens ?? 0);

                    return aiContent;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error calling OpenAI (attempt {Attempt})", attempt);
                    if (attempt < MAX_RETRIES)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    }
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken == default)
                {
                    _logger.LogWarning("OpenAI request timed out (attempt {Attempt})", attempt);
                    if (attempt < MAX_RETRIES)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    }
                }
            }

            _logger.LogError("All {Max} attempts to call OpenAI failed. Returning mock response.", MAX_RETRIES);
            return GetMockResponse(prompt);
        }

        private string GetMockResponse(string prompt)
        {
            // Graceful fallback when API is unavailable
            if (prompt.Contains("Score from 1-10"))
            {
                return "{\"score\": 5, \"reason\": \"Mock score - OpenAI API not configured\"}";
            }

            if (prompt.Contains("Create 5 hooks"))
            {
                return "[\"Hook placeholder 1\", \"Hook placeholder 2\", \"Hook placeholder 3\", \"Hook placeholder 4\", \"Hook placeholder 5\"]";
            }

            return "{\"response\": \"Mock AI Response - configure OPENAI_API_KEY\"}";
        }

        // Request/Response DTOs
        private class OpenAIRequest
        {
            public string Model { get; set; } = string.Empty;
            public Message[] Messages { get; set; } = Array.Empty<Message>();
            public double Temperature { get; set; }
            public int MaxTokens { get; set; }
        }

        private class Message
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }

        private class OpenAIResponse
        {
            public Choice[]? Choices { get; set; }
            public UsageInfo? Usage { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class UsageInfo
        {
            public int TotalTokens { get; set; }
        }
    }
}
