using System.Text.Json;
using StackExchange.Redis;
using API.Models.Common;
using Microsoft.Extensions.Options;
using API.Settings;
using API.Services.Interfaces;

namespace API.Services;

/// <summary>
/// Handles caching of credit card recommendations.
/// Uses Redis for distributed caching with 10-minute expiration.
/// </summary>
public class RedisService : IRedisService
{
    // Cache key format: creditcard:request:{name}:{score}:{salary}
    private const string KeyPrefix = "creditcard:request:";
    private readonly IDatabase _db;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IOptions<RedisSettings> redisSettings, ILogger<RedisService> logger)
    {
        _logger = logger;

        var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { redisSettings.Value.ConnectionString },
            ConnectTimeout = 5000,
            AbortOnConnectFail = false
        });

        _db = redis.GetDatabase();
        _logger.LogInformation("Connected to Redis at {Endpoint}", redisSettings.Value.ConnectionString);
    }

    public Task StoreRequestResults(string name, int score, int salary, List<CreditCardRecommendation> cards)
    {
        return _db.StringSetAsync(
            GetKey(name, score, salary),
            JsonSerializer.Serialize(cards),
            TimeSpan.FromMinutes(10));
    }

    public async Task<List<CreditCardRecommendation>?> GetRequestResults(string name, int score, int salary)
    {
        var value = await _db.StringGetAsync(GetKey(name, score, salary));
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<CreditCardRecommendation>>(value!);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Deserialization failed for cache value");
            return null;
        }
    }

    // Generates consistent cache keys for credit card requests
    private static string GetKey(string name, int score, int salary) =>
        $"{KeyPrefix}{Uri.EscapeDataString(name)}:{score}:{salary}";
}