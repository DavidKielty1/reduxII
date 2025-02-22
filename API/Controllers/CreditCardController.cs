using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Models.Common;
using API.Services;
using API.Models.Responses;
using API.Services.Interfaces;

namespace API.Controllers
{
    /// <summary>
    /// Entry point for credit card recommendation requests.
    /// Handles HTTP requests, caching, and orchestrates the recommendation process.
    /// </summary>
    [ApiController]
    [Route("api/credit-cards")]
    public class CreditCardController : ControllerBase
    {
        private readonly ICreditCardService _service;
        private readonly ILogger<CreditCardController> _logger;

        public CreditCardController(ICreditCardService service, ILogger<CreditCardController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessCreditCard([FromBody] CreditCardRequest request)
        {
            try
            {
                // Get recommendations (from cache or fresh from providers)
                var (cards, fromCache) = await _service.GetRecommendations(request);

                if (!cards.Any())
                {
                    return BadRequest(new ErrorResponse { Message = "No credit card recommendations found" });
                }

                // Return results with source indicator
                return Ok(new CreditCardResponse
                {
                    Message = fromCache ? "Retrieved from cache" : "Fetched from APIs",
                    Cards = cards
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit card request");
                return StatusCode(500, new ErrorResponse { Message = "Internal server error" });
            }
        }
    }
}