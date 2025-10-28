namespace ACO_Microservice.Models.Requests
{
    public class NewItineraryRequest
    {
        public string TripTitle { get; set; } = string.Empty;
        public int SelectedState { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public long HotelPlaceId { get; set; }
        public bool IsCertificatedHotel { get; set; }
    }

    public class NearbyPreferencesRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AllByIdRequest
    {
        public List<long> Place_ids { get; set; } = new();
    }

    public class SaveItineraryRequest
    {
        public string TripTitle { get; set; } = string.Empty;
        public long HotelPlaceId { get; set; }
        public bool IsCertificatedHotel { get; set; }
        public List<ItineraryDayDto> ItineraryDays { get; set; } = new();
    }

    public class ItineraryDayDto
    {
        public string ItineraryDate { get; set; } = string.Empty;
        public List<PlaceVisitDto> Places { get; set; } = new();
    }

    public class PlaceVisitDto
    {
        public long PlaceId { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public int VisitOrder { get; set; }
        public string ArrivalTime { get; set; } = string.Empty;
        public string LeavingTime { get; set; } = string.Empty;
    }
}