using System;

namespace ACO_Microservice.Configuration
{
    public interface IServiceConfiguration
    {
        string ApiHost { get; }
        string PlaceByIdPath { get; }
        string NearbyPreferencesPath { get; }
        string AllByIdPath { get; }
        string PostalCodesPath { get; }
        string SaveItineraryPath { get; }
    }

    public class ServiceConfiguration : IServiceConfiguration
    {
        public string ApiHost => Environment.GetEnvironmentVariable("ApiHost") ?? throw new InvalidOperationException("ApiHost not configured");
        public string PlaceByIdPath => Environment.GetEnvironmentVariable("PlaceByIdPath") ?? "/place";
        public string NearbyPreferencesPath => Environment.GetEnvironmentVariable("NearbyPreferencesPath") ?? "/place/nearby-preferences";
        public string AllByIdPath => Environment.GetEnvironmentVariable("AllByIdPath") ?? "/place/allById";
        public string PostalCodesPath => Environment.GetEnvironmentVariable("PostalCodesPath") ?? "/catalogs/postal-codes";
        public string SaveItineraryPath => Environment.GetEnvironmentVariable("SaveItineraryPath") ?? "/itineraries";
    }
}