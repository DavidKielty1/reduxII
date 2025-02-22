using System.Net;
using System.Text.Json;
using API.Models.CardProviders;
using API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace API.Tests.Services;

public class ApiServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<ApiService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly ApiService _service;

    public ApiServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ApiService>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();

        var client = new HttpClient(_mockHttpHandler.Object);
        _service = new ApiService(client, _mockConfig.Object, _mockLogger.Object);

        // Configure endpoints
        _mockConfig.Setup(x => x["CSCARDS_ENDPOINT"]).Returns("http://test.com/cs");
        _mockConfig.Setup(x => x["SCOREDCARDS_ENDPOINT"]).Returns("http://test.com/scored");
    }

    [Fact]
    public async Task GetCSCards_ReturnsDeserializedResponse()
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };
        var expectedResponse = new List<CSCard>
        {
            new() { CardName = "Test Card", Apr = 15.9, Eligibility = 0.8 }
        };

        SetupMockHttpResponse(JsonSerializer.Serialize(expectedResponse));

        // Act
        var response = await _service.GetCSCards(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response.Cards);
        Assert.Single(response.Cards);
        Assert.Equal("Test Card", response.Cards[0].CardName);
    }

    [Fact]
    public async Task GetScoredCards_HandlesErrorResponse()
    {
        // Arrange
        var request = new ScoredCardsRequest { Name = "Test", Score = 700, Salary = 30000 };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error")
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GetScoredCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCSCards_HandlesTimeout()
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCSCards_HandlesMalformedResponse()
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };
        SetupMockHttpResponse("invalid json {[]");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCSCards_ThrowsWhenEndpointNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(x => x["CSCARDS_ENDPOINT"]).Returns((string?)null);
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetScoredCards_HandlesNonArrayResponse()
    {
        // Arrange
        var request = new ScoredCardsRequest { Name = "Test", Score = 700, Salary = 30000 };
        SetupMockHttpResponse("{\"cards\":{\"someOtherProperty\": \"value\"}}"); // Non-array in cards property

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _service.GetScoredCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCSCards_HandlesNullResponse()
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };
        SetupMockHttpResponse("{\"cards\":null}"); // Wrap null in cards property

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetScoredCards_HandlesEndpointNotConfigured()
    {
        // Arrange
        _mockConfig.Setup(x => x["SCOREDCARDS_ENDPOINT"]).Returns((string?)null);
        var request = new ScoredCardsRequest { Name = "Test", Score = 700, Salary = 30000 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetScoredCards(request, CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetCSCards_HandlesVariousHttpErrors(HttpStatusCode statusCode)
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Error message")
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetCSCards_HandlesNetworkError()
    {
        // Arrange
        var request = new CSCardsRequest { Name = "Test", CreditScore = 700 };
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GetCSCards(request, CancellationToken.None));
    }

    private void SetupMockHttpResponse(string content)
    {
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }
}