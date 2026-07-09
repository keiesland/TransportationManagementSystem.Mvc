namespace TransportationManagementSystem.Mvc.DomainModels
{
    public class TripSegment
    {
        public TimeSpan ScheduledPickupTime { get; set; }
        public TimeSpan PickupArrivalTime { get; set; }
        public TimeSpan ActualPickupTime { get; set; }
        public TimeSpan ActualDropoffTime { get; set; }
        public TimeSpan ScheduledDropoffTime { get; set; }
        public bool IsNoShow { get; set; }
    }
}
