using API.Models.Common;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    /// <summary>
    /// Calculates normalized scores for credit card recommendations.
    /// Handles different scoring scales from various providers.
    /// </summary>
    public static class CardScoreCalculator
    {
        public static List<CreditCardRecommendation> CalculateNormalizedScores(
            List<CreditCardRecommendation> cards,
            ILogger logger)
        {
            foreach (var card in cards)
            {
                // Step 1: Normalize eligibility scores to 0-1 scale
                var normalizedEligibility = card.Provider switch
                {
                    "CSCards" => card.CardScore / 10m,  // CSCards uses 0-10 scale
                    "ScoredCards" => card.CardScore,    // ScoredCards already uses 0-1
                    _ => throw new ArgumentException($"Unknown provider: {card.Provider}")
                };

                // Step 2: Calculate final score using APR weight
                // Formula: eligibility * (1/APR)² * 100
                var aprFactor = 1m / card.Apr;
                var sortingScore = normalizedEligibility * (aprFactor * aprFactor) * 100m;

                // Step 3: Round down to 3 decimal places for consistent comparison
                card.CardScore = Math.Floor(sortingScore * 1000m) / 1000m;
            }

            return cards.OrderByDescending(c => c.CardScore).ToList();
        }
    }
}