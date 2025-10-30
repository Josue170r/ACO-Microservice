using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ACO_Microservice.Models.Requests;
using ACO_Microservice.Services;

namespace ACO_Microservice.Functions
{
    public class ItineraryFunction
    {
        private readonly ILogger<ItineraryFunction> _logger;
        private readonly IItineraryService _itineraryService;
        private readonly IPlaceService _placeService;

        public ItineraryFunction(ILogger<ItineraryFunction> logger, IItineraryService itineraryService, IPlaceService placeService)
        {
            _logger = logger;
            _itineraryService = itineraryService;
            _placeService = placeService;
        }

        [Function("GenerateItinerary")]
        public async Task<IActionResult> GenerateItinerary(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "itinerary/generate")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("GenerateItinerary function triggered");

                // Extract Bearer token from headers
                string? bearerToken = null;
                if (req.Headers.TryGetValue("Authorization", out var authHeader) &&
                    authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    bearerToken = authHeader.ToString().Substring("Bearer ".Length).Trim();
                }

                if (string.IsNullOrEmpty(bearerToken))
                {
                    req.HttpContext.Response.Headers.Append("WWW-Authenticate",
                        "Bearer realm=\"api\", error=\"invalid_token\", error_description=\"Bearer token required\"");
                    return new UnauthorizedResult();
                }

                // Parse request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<NewItineraryRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return new BadRequestObjectResult(new { error = "Invalid request body" });
                }

                // Generate itinerary
                var itinerary = await _itineraryService.GenerateItineraryAsync(request, bearerToken);

                // Save itinerary to backend
                var saved = await _placeService.SaveItineraryAsync(itinerary, bearerToken);

                if (!saved)
                {
                    _logger.LogWarning("Failed to save itinerary to backend");
                }

                return new OkObjectResult(new
                {
                    status = 200,
                    message = "Itinerary generated successfully",
                    data = itinerary,
                    saved
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Business logic error");
                return new BadRequestObjectResult(new
                {
                    status = 400,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating itinerary");
                return new ObjectResult(new
                {
                    status = 500,
                    error = "Internal server error"
                })
                {
                    StatusCode = 500
                };
            }
        }

        [Function("HealthCheck")]
        public IActionResult HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            return new OkObjectResult(new
            {
                status = "Healthy",
                service = "ACO_Microservice",
                timestamp = DateTime.UtcNow
            });
        }
    }
}