using System.Text.Json.Serialization;
using TransportationManagementSystem.Mvc.Data.DTOs;

namespace TransportationManagementSystem.Mvc.Data.DTOs
{
    public class SummaryGridDTO : GridDTO
    {
        [JsonIgnore]
        public const string DefaultFilter = "all";

        public string Driver { get; set; } = DefaultFilter;
        public string TripDate { get; set; } = DefaultFilter;
    }

}
