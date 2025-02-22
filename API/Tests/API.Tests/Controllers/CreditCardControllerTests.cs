using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using API.Controllers;
using API.Models;
using API.Models.Common;
using API.Models.Responses;
using Xunit;
using API.Services.Interfaces;

namespace API.Tests.Controllers
{
    public class CreditCardControllerTests
    {
        private readonly Mock<ICreditCardService> _serviceMock;
        private readonly Mock<ILogger<CreditCardController>> _loggerMock;
        private readonly CreditCardController _controller;

        public CreditCardControllerTests()
        {
            _serviceMock = new Mock<ICreditCardService>();
            _loggerMock = new Mock<ILogger<CreditCardController>>();
            _controller = new CreditCardController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessCreditCard_ReturnsOk_WhenCardsFound()
        {
            // Arrange
            var request = new CreditCardRequest { Name = "Test User", Score = 700 };
            var cards = new List<CreditCardRecommendation>
            {
                new() { Name = "Test Card", Apr = 14.9M, CardScore = 90.0M, Provider = "CSCards" }
            };

            _serviceMock
                .Setup(x => x.GetRecommendations(request))
                .ReturnsAsync((cards, false));

            // Act
            var result = await _controller.ProcessCreditCard(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CreditCardResponse>(okResult.Value);
            Assert.Equal("Fetched from APIs", response.Message);
            Assert.NotNull(response.Cards);
            Assert.Single(response.Cards);
            Assert.Equal("Test Card", response.Cards[0].Name);
        }

        [Fact]
        public async Task ProcessCreditCard_ReturnsBadRequest_WhenNoCardsFound()
        {
            // Arrange
            var request = new CreditCardRequest { Name = "Test User", Score = 700 };

            _serviceMock
                .Setup(x => x.GetRecommendations(request))
                .ReturnsAsync((new List<CreditCardRecommendation>(), false));

            // Act
            var result = await _controller.ProcessCreditCard(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("No credit card recommendations found", response.Message);
        }

        [Fact]
        public async Task ProcessCreditCard_ReturnsServerError_WhenExceptionOccurs()
        {
            // Arrange
            var request = new CreditCardRequest { Name = "Test User", Score = 700 };

            _serviceMock
                .Setup(x => x.GetRecommendations(request))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.ProcessCreditCard(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ErrorResponse>(statusCodeResult.Value);
            Assert.Equal("Internal server error", response.Message);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }
    }
}