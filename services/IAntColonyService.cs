using ACO_Microservice.Models.Responses;
using ACO_Microservice.Models.Requests;

namespace ACO_Microservice.Services
{
    public interface IAntColonyService
    {
        SaveItineraryRequest OptimizeItinerary(
            PlaceData hotel,
            List<PlaceData> places,
            DateTime startDate,
            DateTime endDate,
            string tripTitle,
            bool isCertificatedHotel);
    }
}