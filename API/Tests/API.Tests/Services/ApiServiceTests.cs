using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using API.Models.CardProviders;
using API.Services;
using Xunit;

namespace API.Tests.Services
{
    public class ApiServiceTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<ApiService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly ApiService _service;

        public ApiServiceTests()
        {
            // Setup configuration mock
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(x => x["CSCARDS_ENDPOINT"]).Returns("http://test.com/cs");
            _configMock.Setup(x => x["SCOREDCARDS_ENDPOINT"]).Returns("http://test.com/scored");

            // Setup logger mock
            _loggerMock = new Mock<ILogger<ApiService>>();

            // Setup HTTP handler mock
            _handlerMock = new Mock<HttpMessageHandler>();

            // Create HTTP client with mocked handler
            var client = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _service = new ApiService(client, _configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetCSCards_Success_ReturnsCards()
        {
            // Arrange
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{\"cardName\":\"Test Card\",\"apr\":14.9,\"eligibility\":7.5}]")
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetCSCards(new CSCardsRequest { Name = "Test", CreditScore = 700 }, default);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Cards);
            Assert.Equal("Test Card", result.Cards[0].CardName);
            Assert.Equal(14.9, result.Cards[0].Apr);
            Assert.Equal(7.5, result.Cards[0].Eligibility);
        }

        [Fact]
        public async Task GetCSCards_ApiError_ThrowsException()
        {
            // Arrange
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _service.GetCSCards(new CSCardsRequest { Name = "Test", CreditScore = 700 }, default));
        }
    }
}