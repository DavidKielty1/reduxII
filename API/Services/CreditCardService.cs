using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using API.Models;
using API.Models.Common;
using API.Models.CardProviders;
using API.Services;

namespace API.Services
{
    /// <summary>
    /// Core business logic for credit card recommendations.
    /// Manages caching strategy and coordinates between different card providers.
    /// </summary>
    public class CreditCardService
    {
        private readonly RedisService _cache;
        private readonly CardProviderService _cardProvider;
        private readonly ILogger<CreditCardService> _logger;

        public CreditCardService(
            RedisService cache,
            CardProviderService cardProvider,
            ILogger<CreditCardService> logger)
        {
            _cache = cache;
            _cardProvider = cardProvider;
            _logger = logger;
        }

        public async Task<(List<CreditCardRecommendation> cards, bool fromCache)> GetRecommendations(CreditCardRequest request)
        {
            // Step 1: Try to get cached results first
            var cached = await _cache.GetRequestResults(request.Name, request.Score, request.Salary);
            if (cached?.Any() == true)
            {
                return (cached, true);
            }

            // Step 2: Get fresh recommendations from providers
            var cards = await _cardProvider.GetAllRecommendations(request, default);
            if (cards.Any())
            {
                // Step 3: Calculate scores and cache the results
                var scored = CardScoreCalculator.CalculateNormalizedScores(cards, _logger);
                await _cache.StoreRequestResults(request.Name, request.Score, request.Salary, scored);
                return (scored, false);
            }

            return (new List<CreditCardRecommendation>(), false);
        }
    }
}