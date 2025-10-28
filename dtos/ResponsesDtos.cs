namespace ACO_Microservice.Models.Responses
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }

    public class PlaceData
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string PlaceId { get; set; } = string.Empty;
        public string FormattedAddress { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? FormattedPhoneNumber { get; set; }
        public List<string> PlaceTypes { get; set; } = new();
        public double? LocalRating { get; set; }
        
        // Runtime properties
        public double SustainabilityIndex { get; set; }
        public string? PostalCode { get; set; }
    }

    public class AllByIdResponse
    {
        public List<PlaceData> Content { get; set; } = new();
        public int TotalElements { get; set; }
        public int NumberOfElements { get; set; }
        public int Size { get; set; }
        public bool Empty { get; set; }
    }

    public class PostalCodeData
    {
        public int Id { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public SettlementData Settlement { get; set; } = new();
    }

    public class SettlementData
    {
        public int Id { get; set; }
        public string Settlement { get; set; } = string.Empty;
        public double SustainabilityIndex { get; set; }
        public StateData State { get; set; } = new();
    }

    public class StateData
    {
        public int Id { get; set; }
        public string State { get; set; } = string.Empty;
    }
}