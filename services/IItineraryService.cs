using ACO_Microservice.Models.Requests;

namespace ACO_Microservice.Services
{
    public interface IItineraryService
    {
        Task<SaveItineraryRequest> GenerateItineraryAsync(NewItineraryRequest request, string? bearerToken);
    }
}