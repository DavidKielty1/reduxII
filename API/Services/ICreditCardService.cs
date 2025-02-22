using API.Models;
using API.Models.Common;

namespace API.Services
{
    public interface ICreditCardService
    {
        Task<(List<CreditCardRecommendation> cards, bool fromCache)> GetRecommendations(CreditCardRequest request);
    }
}