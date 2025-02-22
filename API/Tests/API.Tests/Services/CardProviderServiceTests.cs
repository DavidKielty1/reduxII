using API.Models;
using API.Models.CardProviders;
using API.Models.Common;
using API.Services;
using API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Services;

public class CardProviderServiceTests
{
    private readonly Mock<IApiService> _mockApi;
    private readonly Mock<ILogger<CardProviderService>> _mockLogger;
    private readonly CardProviderService _service;

    public CardProviderServiceTests()
    {
        _mockApi = new Mock<IApiService>();
        _mockLogger = new Mock<ILogger<CardProviderService>>();
        _service = new CardProviderService(_mockApi.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllRecommendations_ReturnsAggregatedResults()
    {
        // Arrange
        var request = new CreditCardRequest { Name = "Test", Score = 700, Salary = 30000 };

        _mockApi.Setup(x => x.GetCSCards(It.IsAny<CSCardsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CSCardsResponse
            {
                Cards = new List<CSCard>
                {
                    new() { CardName = "CS Card", Apr = 15.9, Eligibility = 8.5 }
                }
            });

        _mockApi.Setup(x => x.GetScoredCards(It.IsAny<ScoredCardsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScoredCardsResponse
            {
                Cards = new List<ScoredCard>
                {
                    new() { Card = "Scored Card", Apr = 14.9, ApprovalRating = 0.95 }
                }
            });

        // Act
        var results = await _service.GetAllRecommendations(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, c => c.Provider == "CSCards" && c.Name == "CS Card");
        Assert.Contains(results, c => c.Provider == "ScoredCards" && c.Name == "Scored Card");
    }

    [Fact]
    public async Task GetAllRecommendations_HandlesProviderFailure()
    {
        // Arrange
        var request = new CreditCardRequest { Name = "Test", Score = 700, Salary = 30000 };

        _mockApi.Setup(x => x.GetCSCards(It.IsAny<CSCardsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        _mockApi.Setup(x => x.GetScoredCards(It.IsAny<ScoredCardsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScoredCardsResponse
            {
                Cards = new List<ScoredCard>
                {
                    new() { Card = "Scored Card", Apr = 14.9, ApprovalRating = 0.95 }
                }
            });

        // Act
        var results = await _service.GetAllRecommendations(request, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Contains(results, c => c.Provider == "ScoredCards");
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetAllRecommendations_HandlesTimeout()
    {
        // Arrange
        var request = new CreditCardRequest { Name = "Test", Score = 700, Salary = 30000 };

        // Mock both API calls to be slow
        _mockApi.Setup(x => x.GetCSCards(It.IsAny<CSCardsRequest>(), It.IsAny<CancellationToken>()))
            .Returns<CSCardsRequest, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(5000, ct); // Longer than the 3-second timeout
                return new CSCardsResponse();
            });

        _mockApi.Setup(x => x.GetScoredCards(It.IsAny<ScoredCardsRequest>(), It.IsAny<CancellationToken>()))
            .Returns<ScoredCardsRequest, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(5000, ct); // Longer than the 3-second timeout
                return new ScoredCardsResponse();
            });

        // Act
        var results = await _service.GetAllRecommendations(request, CancellationToken.None);

        // Assert
        Assert.Empty(results);

        // Verify that error logs were created for both providers
        Func<object, Type, bool> state1Matcher = (v, _) => v.ToString()!.Contains("CSCards API failed");
        Func<object, Type, bool> state2Matcher = (v, _) => v.ToString()!.Contains("ScoredCards API failed");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => state1Matcher(o, t)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => state2Matcher(o, t)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}