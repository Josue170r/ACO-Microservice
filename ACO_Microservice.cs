using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TT_2025_B077_ACO;

public class ACO_Microservice
{
    private readonly ILogger<ACO_Microservice> _logger;

    public ACO_Microservice(ILogger<ACO_Microservice> logger)
    {
        _logger = logger;
    }

    [Function("ACO_Microservice")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}