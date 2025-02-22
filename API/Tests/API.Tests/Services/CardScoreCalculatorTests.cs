using API.Models.Common;
using API.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Services;

public class CardScoreCalculatorTests
{
    private readonly ILogger _logger;

    public CardScoreCalculatorTests()
    {
        _logger = Mock.Of<ILogger>();
    }

    [Fact]
    public void CalculateNormalizedScores_CSCards_NormalizesCorrectly()
    {
        // Arrange
        var cards = new List<CreditCardRecommendation>
        {
            new() { Provider = "CSCards", Name = "Test Card", Apr = 20.0m, CardScore = 8.0m }
        };

        // Act
        var result = CardScoreCalculator.CalculateNormalizedScores(cards, _logger);

        // Assert
        Assert.Single(result);
        var card = result[0];
        // CSCards score (8.0) normalized to 0-1 scale (8.0/10 = 0.8)
        // Formula: 0.8 * (1/20)² * 100 = 0.2
        Assert.Equal(0.2m, card.CardScore);
    }

    [Fact]
    public void CalculateNormalizedScores_ScoredCards_MaintainsScale()
    {
        // Arrange
        var cards = new List<CreditCardRecommendation>
        {
            new() { Provider = "ScoredCards", Name = "Test Card", Apr = 15.0m, CardScore = 0.7m }
        };

        // Act
        var result = CardScoreCalculator.CalculateNormalizedScores(cards, _logger);

        // Assert
        Assert.Single(result);
        var card = result[0];
        // ScoredCards already uses 0-1 scale (0.7)
        // Formula: 0.7 * (1/15)² * 100 = 0.311
        Assert.Equal(0.311m, card.CardScore);
    }

    [Fact]
    public void CalculateNormalizedScores_OrdersByScoreDescending()
    {
        // Arrange
        var cards = new List<CreditCardRecommendation>
        {
            new() { Provider = "CSCards", Name = "Lower Score", Apr = 20.0m, CardScore = 5.0m },
            new() { Provider = "ScoredCards", Name = "Higher Score", Apr = 15.0m, CardScore = 0.9m }
        };

        // Act
        var result = CardScoreCalculator.CalculateNormalizedScores(cards, _logger);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Higher Score", result[0].Name);
        Assert.Equal("Lower Score", result[1].Name);
    }
}