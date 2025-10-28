using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ACO_Microservice.Services;
using ACO_Microservice.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<IServiceConfiguration, ServiceConfiguration>();

        services.AddHttpClient<IPlaceService, PlaceService>((serviceProvider, client) =>
        {
            var config = serviceProvider.GetRequiredService<IServiceConfiguration>();
            var baseUrl = config.ApiHost.TrimEnd('/');
            client.BaseAddress = new Uri($"{baseUrl}/api");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddScoped<IAntColonyService, AntColonyService>();
        services.AddScoped<IItineraryService, ItineraryService>();
    })
    .Build();

host.Run();