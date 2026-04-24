using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using DecisionEngine;
using DecisionEngine.Infrastructure.Database;
using DecisionEngine.Infrastructure.FacebookApi;
using DecisionEngine.Infrastructure.AI;
using DecisionEngine.Core.Interfaces;
using DecisionEngine.Core.Entities;
using DecisionEngine.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=fb_ads_system;Username=user;Password=password";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// Add HttpClientFactory
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Add Facebook Config
var fbConfig = new FacebookConfig();
builder.Configuration.GetSection("Facebook").Bind(fbConfig);
builder.Services.AddSingleton(fbConfig);

builder.Services.AddScoped<FacebookApiClient>();

// Add AI Service (with proper DI)
var openAiKey = builder.Configuration.GetValue<string>("AI:OpenAIKey") ?? "MISSING_KEY";
builder.Services.AddSingleton<ILLMService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<OpenAIService>>();
    return new OpenAIService(openAiKey, httpClientFactory, logger);
});

// Add Services
builder.Services.AddScoped<TrafficService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CreativeService>();
builder.Services.AddScoped<DecisionService>();
builder.Services.AddScoped<TrackingService>();
builder.Services.AddScoped<OrchestrationService>();
builder.Services.AddHostedService<Worker>();

// Add CORS for dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Dashboard");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
