using TransportationManagementSystem.Mvc.DomainModels;

namespace TransportationManagementSystem.Mvc.Data.DTOs
{
    public class SummaryResult
    {
        public List<DriverDaySummary> Summaries { get; set; } = new();
    }
}
