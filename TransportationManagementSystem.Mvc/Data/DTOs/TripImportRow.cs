namespace TransportationManagementSystem.Mvc.Data.DTOs
{
    public class TripImportRow
    {
        public string Driver { get; set; }
        public DateTime TripDate { get; set; }
        public TimeSpan TripActualStartTime { get; set; }
        public TimeSpan ScheduledPickupTime { get; set; }
        public TimeSpan PickupArrivalTime { get; set; }
        public TimeSpan ActualPickupTime { get; set; }
        public TimeSpan ActualDropoffTime { get; set; }
        public TimeSpan ScheduledDropoffTime { get; set; }
        public TimeSpan TripActualEndTime { get; set; }
        public int WeekNumber { get; set; }
    }
}
