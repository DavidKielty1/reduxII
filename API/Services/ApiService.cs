using System.Text.Json;
using API.Models.CardProviders;
using API.Services.Interfaces;

namespace API.Services
{
    /// <summary>
    /// Low-level HTTP client for credit card provider APIs.
    /// Handles raw HTTP communication and response deserialization.
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiService> _logger;

        public ApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configure HTTP client defaults
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ClearScore-Test/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.Timeout = TimeSpan.FromSeconds(3);
        }

        public async Task<CSCardsResponse> GetCSCards(CSCardsRequest request, CancellationToken cancellationToken)
        {
            var endpoint = _configuration["CSCARDS_ENDPOINT"]
                ?? throw new InvalidOperationException("CSCARDS_ENDPOINT not set");
            return await SendRequest<CSCardsResponse>(endpoint, request, cancellationToken);
        }

        public async Task<ScoredCardsResponse> GetScoredCards(ScoredCardsRequest request, CancellationToken cancellationToken)
        {
            var endpoint = _configuration["SCOREDCARDS_ENDPOINT"]
                ?? throw new InvalidOperationException("SCOREDCARDS_ENDPOINT not set");
            return await SendRequest<ScoredCardsResponse>(endpoint, request, cancellationToken);
        }

        private async Task<T> SendRequest<T>(string endpoint, object request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                // Special handling: Wrap array responses in an object with 'cards' property
                if (typeof(T) == typeof(CSCardsResponse) || typeof(T) == typeof(ScoredCardsResponse))
                {
                    content = $"{{\"cards\":{content}}}";
                }

                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException($"Failed to deserialize response");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling {Endpoint}", endpoint);
                throw;
            }
        }
    }
}