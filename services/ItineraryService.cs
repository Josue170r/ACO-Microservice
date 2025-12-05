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

        private readonly HashSet<string> _hotelTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "lodging",
            "hotel",
            "motel",
            "hostel",
            "resort",
            "bed_and_breakfast",
            "guest_house",
            "aparthotel",
            "inn",
            "campground",
            "rv_park"
        };

        public ItineraryService(IPlaceService placeService, IAntColonyService antColonyService, ILogger<ItineraryService> logger)
        {
            _placeService = placeService;
            _antColonyService = antColonyService;
            _logger = logger;
        }

        public async Task<SaveItineraryRequest> GenerateItineraryAsync(NewItineraryRequest request, string? bearerToken)
        {
            var hotel = await _placeService.GetPlaceByIdAsync(request.HotelPlaceId, bearerToken);
            if (hotel == null)
            {
                throw new InvalidOperationException($"No se encontró el hotel con ID {request.HotelPlaceId}.");
            }

            _logger.LogInformation($"Hotel encontrado: {hotel.Name} en ({hotel.Lat}, {hotel.Lng})");

            var nearbyPlaceIds = await _placeService.GetNearbyPlacesAsync(hotel.Lat, hotel.Lng, bearerToken);
            if (!nearbyPlaceIds.Any())
            {
                throw new InvalidOperationException(
                    "No se encontraron lugares cercanos. Intenta con otro hotel o ajusta tus preferencias.");
            }

            _logger.LogInformation($"Se encontraron {nearbyPlaceIds.Count} lugares cercanos");

            var allPlaces = await _placeService.GetAllPlacesByIdsAsync(nearbyPlaceIds, bearerToken);
            _logger.LogInformation($"Se obtuvieron detalles de {allPlaces.Count} lugares");

            var filteredPlaces = FilterPlaces(allPlaces);
            _logger.LogInformation($"Después de filtrar hoteles y duplicados: {filteredPlaces.Count} lugares");

            if (!filteredPlaces.Any())
            {
                throw new InvalidOperationException(
                    "Solo se encontraron hoteles. Modifica tus preferencias para incluir otros tipos de lugares.");
            }

            var validPlaces = new List<PlaceData>();
            foreach (var place in filteredPlaces)
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

            _logger.LogInformation($"Lugares con índice de sostenibilidad: {validPlaces.Count}");

            if (!validPlaces.Any())
            {
                throw new InvalidOperationException(
                    "No se encontraron lugares con datos de sostenibilidad. Intenta con otro hotel.");
            }

            if (!DateTime.TryParse(request.StartDate, out DateTime startDate) ||
                !DateTime.TryParse(request.EndDate, out DateTime endDate))
            {
                throw new ArgumentException("Formato de fecha inválido. Usa YYYY-MM-DD.");
            }

            var totalDays = (endDate - startDate).Days + 1;
            var minimumPlacesNeeded = Math.Min(totalDays * 2, 10);

            if (validPlaces.Count < minimumPlacesNeeded)
            {
                throw new InvalidOperationException(
                    $"Solo se encontraron {validPlaces.Count} lugares válidos, se necesitan al menos {minimumPlacesNeeded}. " +
                    "Modifica tus preferencias o reduce los días del viaje.");
            }

            _logger.LogInformation($"Ejecutando ACO con {validPlaces.Count} lugares para {totalDays} días");
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

        private List<PlaceData> FilterPlaces(List<PlaceData> places)
        {
            var filteredPlaces = new List<PlaceData>();
            var seenPlaceIds = new HashSet<string>();

            foreach (var place in places)
            {
                if (IsHotel(place))
                {
                    _logger.LogDebug($"Excluyendo hotel: {place.Name}");
                    continue;
                }

                if (!string.IsNullOrEmpty(place.PlaceId) && seenPlaceIds.Contains(place.PlaceId))
                {
                    _logger.LogDebug($"Excluyendo duplicado: {place.Name}");
                    continue;
                }

                filteredPlaces.Add(place);
                if (!string.IsNullOrEmpty(place.PlaceId))
                {
                    seenPlaceIds.Add(place.PlaceId);
                }
            }

            return filteredPlaces;
        }

        private bool IsHotel(PlaceData place)
        {
            if (place.PlaceTypes?.Any(type => _hotelTypes.Contains(type)) == true)
            {
                return true;
            }

            var lowerName = place.Name?.ToLower() ?? "";
            return lowerName.Contains("hotel") || 
                   lowerName.Contains("motel") || 
                   lowerName.Contains("hostel") ||
                   lowerName.Contains("resort") ||
                   lowerName.Contains("inn") ||
                   lowerName.Contains("suites");
        }

        private string? ExtractPostalCode(string? formattedAddress)
        {
            if (string.IsNullOrEmpty(formattedAddress)) return null;

            var match = Regex.Match(formattedAddress, @"\b(\d{5})\b");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}