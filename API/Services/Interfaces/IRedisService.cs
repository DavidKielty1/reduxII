using API.Models.Common;

namespace API.Services.Interfaces
{
    public interface IRedisService
    {
        Task StoreRequestResults(string name, int score, int salary, List<CreditCardRecommendation> cards);
        Task<List<CreditCardRecommendation>?> GetRequestResults(string name, int score, int salary);
    }
}