namespace TransportationManagementSystem.Mvc.DomainModels
{
    public class DriverDaySummary
    {
        public string Driver { get; set; }
        public DateTime RideDate { get; set; }
        public int WeekNumber { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public TimeSpan PaidTime { get; set; }
        public TimeSpan? WeeklyTime { get; set; }
        public List<(TimeSpan Out, TimeSpan In)> Breaks { get; set; } = new();
    }
}
