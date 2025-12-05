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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "itinerary/generate")] HttpRequest req)
        {
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
            req.HttpContext.Response.Headers.Append("Access-Control-Max-Age", "3600");

            if (req.Method == "OPTIONS")
            {
                return new OkResult();
            }

            try
            {
                _logger.LogInformation("Función GenerateItinerary iniciada");

                string? bearerToken = null;
                if (req.Headers.TryGetValue("Authorization", out var authHeader) &&
                    authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    bearerToken = authHeader.ToString().Substring("Bearer ".Length).Trim();
                }

                if (string.IsNullOrEmpty(bearerToken))
                {
                    req.HttpContext.Response.Headers.Append("WWW-Authenticate",
                        "Bearer realm=\"api\", error=\"invalid_token\", error_description=\"Token de autorización requerido\"");
                    return new UnauthorizedResult();
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonSerializer.Deserialize<NewItineraryRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return new BadRequestObjectResult(new { error = "Cuerpo de solicitud inválido" });
                }

                var itinerary = await _itineraryService.GenerateItineraryAsync(request, bearerToken);

                var saved = await _placeService.SaveItineraryAsync(itinerary, bearerToken);

                if (!saved)
                {
                    _logger.LogWarning("No se pudo guardar el itinerario en el backend");
                }

                return new OkObjectResult(new
                {
                    status = 200,
                    message = "Itinerario generado exitosamente",
                    data = itinerary,
                    saved
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error de lógica de negocio");
                return new BadRequestObjectResult(new
                {
                    status = 400,
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al generar itinerario");
                return new ObjectResult(new
                {
                    status = 500,
                    error = "Error interno del servidor"
                })
                {
                    StatusCode = 500
                };
            }
        }

        [Function("HealthCheck")]
        public IActionResult HealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "health")] HttpRequest req)
        {
            if (req.Method == "OPTIONS")
            {
                req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                req.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
                req.HttpContext.Response.Headers.Append("Access-Control-Max-Age", "3600");
                return new OkResult();
            }

            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");

            return new OkObjectResult(new
            {
                status = "Saludable",
                service = "ACO_Microservice",
                timestamp = DateTime.UtcNow
            });
        }
    }
}