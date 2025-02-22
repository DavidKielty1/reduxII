using API.Models.CardProviders;

namespace API.Services.Interfaces
{
    public interface IApiService
    {
        Task<CSCardsResponse> GetCSCards(CSCardsRequest request, CancellationToken cancellationToken);
        Task<ScoredCardsResponse> GetScoredCards(ScoredCardsRequest request, CancellationToken cancellationToken);
    }
}