using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ACO_Microservice.Configuration;
using ACO_Microservice.Models.Requests;
using ACO_Microservice.Models.Responses;
using Microsoft.Extensions.Logging;

namespace ACO_Microservice.Services
{
    public class PlaceService : IPlaceService
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceConfiguration _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<PlaceService> _logger;

        public PlaceService(HttpClient httpClient, IServiceConfiguration config, ILogger<PlaceService> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private void SetBearerToken(string? bearerToken)
        {
            if (!string.IsNullOrEmpty(bearerToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", bearerToken);
            }
        }

        public async Task<PlaceData?> GetPlaceByIdAsync(long placeId, string? bearerToken = null)
        {
            SetBearerToken(bearerToken);
            var url = $"{_httpClient.BaseAddress}{_config.PlaceByIdPath}/{placeId}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<PlaceData>>(content, _jsonOptions);
                return apiResponse?.Data;
            }

            return null;
        }

        public async Task<List<long>> GetNearbyPlacesAsync(double latitude, double longitude, string? bearerToken = null)
        {
            SetBearerToken(bearerToken);
            var request = new NearbyPreferencesRequest
            {
                Latitude = latitude,
                Longitude = longitude
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_httpClient.BaseAddress}{_config.NearbyPreferencesPath}";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<long>>>(responseContent, _jsonOptions);
                return apiResponse?.Data ?? new List<long>();
            }

            return new List<long>();
        }

        public async Task<List<PlaceData>> GetAllPlacesByIdsAsync(List<long> placeIds, string? bearerToken = null)
        {
            if (!placeIds.Any()) return new List<PlaceData>();

            SetBearerToken(bearerToken);
            var request = new AllByIdRequest { Place_ids = placeIds };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_httpClient.BaseAddress}{_config.AllByIdPath}?size={placeIds.Count}";
            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AllByIdResponse>(responseContent, _jsonOptions);
                return apiResponse?.Content ?? new List<PlaceData>();
            }

            return new List<PlaceData>();
        }

        public async Task<double?> GetSustainabilityIndexAsync(string postalCode, int stateId, string? bearerToken = null)
        {
            SetBearerToken(bearerToken);
            var url = $"{_httpClient.BaseAddress}{_config.PostalCodesPath}?postal_code={postalCode}&state={stateId}";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<PostalCodeData>>>(content, _jsonOptions);
                return apiResponse?.Data?.FirstOrDefault()?.Settlement?.SustainabilityIndex;
            }

            return null;
        }

        public async Task<bool> SaveItineraryAsync(SaveItineraryRequest itinerary, string? bearerToken = null)
        {
            SetBearerToken(bearerToken);
            var json = JsonSerializer.Serialize(itinerary, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_httpClient.BaseAddress}{_config.SaveItineraryPath}";
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
    }
}