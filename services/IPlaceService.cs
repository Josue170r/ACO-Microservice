using ACO_Microservice.Models.Responses;

namespace ACO_Microservice.Services
{
    public interface IPlaceService
    {
        Task<PlaceData?> GetPlaceByIdAsync(long placeId, string? bearerToken = null);
        Task<List<long>> GetNearbyPlacesAsync(double latitude, double longitude, string? bearerToken = null);
        Task<List<PlaceData>> GetAllPlacesByIdsAsync(List<long> placeIds, string? bearerToken = null);
        Task<double?> GetSustainabilityIndexAsync(string postalCode, int stateId, string? bearerToken = null);
        Task<bool> SaveItineraryAsync(Models.Requests.SaveItineraryRequest itinerary, string? bearerToken = null);
    }
}