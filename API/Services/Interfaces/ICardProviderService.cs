using API.Models;
using API.Models.Common;

namespace API.Services.Interfaces
{
    public interface ICardProviderService
    {
        Task<List<CreditCardRecommendation>> GetAllRecommendations(CreditCardRequest request, CancellationToken ct);
    }
}