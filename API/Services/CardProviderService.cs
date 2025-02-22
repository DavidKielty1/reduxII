using API.Models;
using API.Models.CardProviders;
using API.Models.Common;
using API.Services.Interfaces;

namespace API.Services
{
    /// <summary>
    /// Manages communication with different credit card providers.
    /// Normalizes responses into a common format.
    /// </summary>
    public class CardProviderService : ICardProviderService
    {
        // Provider identifiers
        private const string CSCardsProvider = "CSCards";
        private const string ScoredCardsProvider = "ScoredCards";
        private const int ProviderTimeout = 3;  // seconds

        private readonly IApiService _api;
        private readonly ILogger<CardProviderService> _logger;
        private readonly Dictionary<string, Func<CreditCardRequest, CancellationToken, Task<List<CreditCardRecommendation>>>> _providers;

        public CardProviderService(IApiService api, ILogger<CardProviderService> logger)
        {
            _api = api;
            _logger = logger;

            // Register available providers
            _providers = new()
            {
                [CSCardsProvider] = GetCSCardsRecommendations,
                [ScoredCardsProvider] = GetScoredCardsRecommendations
            };
        }

        // Fetches recommendations from all providers in parallel
        public async Task<List<CreditCardRecommendation>> GetAllRecommendations(CreditCardRequest request, CancellationToken ct)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(ProviderTimeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            try
            {
                var tasks = _providers.Select(p => GetProviderCards(p.Key, p.Value, request, linkedCts.Token));
                var results = await Task.WhenAll(tasks);
                return results.SelectMany(x => x).ToList();
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Provider APIs timed out after {Timeout} seconds", ProviderTimeout);
                return new List<CreditCardRecommendation>();
            }
        }

        private async Task<List<CreditCardRecommendation>> GetCSCardsRecommendations(CreditCardRequest request, CancellationToken ct)
        {
            var csCardsRequest = new CSCardsRequest
            {
                Name = request.Name,
                CreditScore = request.Score
            };

            var response = await _api.GetCSCards(csCardsRequest, ct);
            return response.Cards.Select(card => new CreditCardRecommendation
            {
                Provider = "CSCards",
                Name = card.CardName,
                Apr = Convert.ToDecimal(card.Apr),
                CardScore = Convert.ToDecimal(card.Eligibility)
            }).ToList();
        }

        private async Task<List<CreditCardRecommendation>> GetScoredCardsRecommendations(CreditCardRequest request, CancellationToken ct)
        {
            var scoredCardsRequest = new ScoredCardsRequest
            {
                Name = request.Name,
                Score = request.Score,
                Salary = request.Salary
            };

            var response = await _api.GetScoredCards(scoredCardsRequest, ct);
            return response.Cards.Select(card => new CreditCardRecommendation
            {
                Provider = "ScoredCards",
                Name = card.Card,
                Apr = Convert.ToDecimal(card.Apr),
                CardScore = Convert.ToDecimal(card.ApprovalRating)
            }).ToList();
        }

        private async Task<List<CreditCardRecommendation>> GetProviderCards(
            string provider,
            Func<CreditCardRequest, CancellationToken, Task<List<CreditCardRecommendation>>> getCards,
            CreditCardRequest request,
            CancellationToken ct)
        {
            try
            {
                return await getCards(request, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Provider} API failed", provider);
                return new List<CreditCardRecommendation>();
            }
        }
    }
}