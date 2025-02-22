using System.Text.Json;
using API.Models.Common;
using API.Services;
using API.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace API.Tests.Services;

public class RedisServiceTests
{
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<ILogger<RedisService>> _mockLogger;
    private readonly RedisService _service;

    public RedisServiceTests()
    {
        // Setup Redis mocks
        _mockDatabase = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisService>>();

        var settings = Options.Create(new RedisSettings
        {
            ConnectionString = "localhost:6379"
        });

        _service = new RedisService(settings, _mockLogger.Object);

        // Use reflection to inject mock database
        var field = typeof(RedisService).GetField("_db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(_service, _mockDatabase.Object);
    }

    [Fact]
    public async Task GetRequestResults_ReturnsDeserializedData()
    {
        // Arrange
        var cards = new List<CreditCardRecommendation>
        {
            new() { Name = "Test Card", Provider = "CSCards", Apr = 15.9m, CardScore = 8.5m }
        };
        var serialized = System.Text.Json.JsonSerializer.Serialize(cards);

        _mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(serialized);

        // Act
        var result = await _service.GetRequestResults("Test", 700, 30000);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Test Card", result[0].Name);
    }

    [Fact]
    public async Task StoreRequestResults_SerializesAndStoresData()
    {
        // Arrange
        var cards = new List<CreditCardRecommendation>
        {
            new() { Name = "Test Card", Provider = "CSCards", Apr = 15.9m, CardScore = 8.5m }
        };

        // Act
        await _service.StoreRequestResults("Test", 700, 30000, cards);

        // Assert
        _mockDatabase.Verify(
            x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRequestResults_HandlesCacheMiss()
    {
        // Arrange
        _mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.GetRequestResults("Test", 700, 30000);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRequestResults_HandlesDeserializationError()
    {
        // Arrange
        _mockDatabase
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("invalid json");

        // Act
        var result = await _service.GetRequestResults("Test", 700, 30000);

        // Assert
        Assert.Null(result);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deserialization failed")),
                It.IsAny<JsonException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StoreRequestResults_HandlesConnectionFailure()
    {
        // Arrange
        _mockDatabase
            .Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var cards = new List<CreditCardRecommendation>
        {
            new() { Name = "Test Card", Provider = "CSCards", Apr = 15.9m, CardScore = 8.5m }
        };

        // Act & Assert
        await Assert.ThrowsAsync<RedisConnectionException>(() =>
            _service.StoreRequestResults("Test", 700, 30000, cards));
    }
}