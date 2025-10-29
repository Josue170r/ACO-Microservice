using System.Text.RegularExpressions;
using ACO_Microservice.Models.Requests;
using ACO_Microservice.Models.Responses;
using Microsoft.Extensions.Logging;

namespace ACO_Microservice.Services
{
    public class ItineraryService : IItineraryService
    {
        private readonly IPlaceService _placeService;
        private readonly IAntColonyService _antColonyService;
        private readonly ILogger<ItineraryService> _logger;

        public ItineraryService(IPlaceService placeService, IAntColonyService antColonyService, ILogger<ItineraryService> logger)
        {
            _placeService = placeService;
            _antColonyService = antColonyService;
            _logger = logger;
        }

        public async Task<SaveItineraryRequest> GenerateItineraryAsync(NewItineraryRequest request, string? bearerToken)
        {
            // 1. Get hotel details
            var hotel = await _placeService.GetPlaceByIdAsync(request.HotelPlaceId, bearerToken);
            if (hotel == null)
            {
                throw new InvalidOperationException($"Hotel with ID {request.HotelPlaceId} not found");
            }

            // 2. Get nearby places
            var nearbyPlaceIds = await _placeService.GetNearbyPlacesAsync(hotel.Lat, hotel.Lng, bearerToken);
            if (!nearbyPlaceIds.Any())
            {
                throw new InvalidOperationException("No nearby places found");
            }

            // 3. Get place details
            var allPlaces = await _placeService.GetAllPlacesByIdsAsync(nearbyPlaceIds, bearerToken);

            // 4. Filter places by sustainability
            var validPlaces = new List<PlaceData>();
            foreach (var place in allPlaces)
            {
                var postalCode = ExtractPostalCode(place.FormattedAddress);
                if (!string.IsNullOrEmpty(postalCode))
                {
                    var sustainabilityIndex = await _placeService.GetSustainabilityIndexAsync(
                        postalCode, request.SelectedState, bearerToken);
                    
                    if (sustainabilityIndex.HasValue)
                    {
                        place.SustainabilityIndex = sustainabilityIndex.Value;
                        place.PostalCode = postalCode;
                        validPlaces.Add(place);
                    }
                }
            }

            if (!validPlaces.Any())
            {
                throw new InvalidOperationException("No places with sustainability data found");
            }

            // 5. Parse dates
            if (!DateTime.TryParse(request.StartDate, out DateTime startDate) ||
                !DateTime.TryParse(request.EndDate, out DateTime endDate))
            {
                throw new ArgumentException("Invalid date format. Use YYYY-MM-DD");
            }

            // 6. Run ACO algorithm
            var itinerary = _antColonyService.OptimizeItinerary(
                hotel,
                validPlaces,
                startDate,
                endDate,
                request.TripTitle,
                request.IsCertificatedHotel
            );

            return itinerary;
        }

        private string? ExtractPostalCode(string? formattedAddress)
        {
            if (string.IsNullOrEmpty(formattedAddress)) return null;

            // Mexican postal codes are 5 digits
            var match = Regex.Match(formattedAddress, @"\b(\d{5})\b");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}