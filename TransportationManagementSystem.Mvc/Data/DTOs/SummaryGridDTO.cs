using System.Text.Json.Serialization;

namespace TransportationManagementSystem.Data.DTOs
{
    public class SummaryGridDTO : GridDTO
    {
        [JsonIgnore]
        public const string DefaultFilter = "all";

        public string Driver { get; set; } = DefaultFilter;
        public string TripDate { get; set; } = DefaultFilter;
    }

}
