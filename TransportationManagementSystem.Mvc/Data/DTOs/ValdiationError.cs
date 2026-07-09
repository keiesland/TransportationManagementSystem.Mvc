namespace TransportationManagementSystem.Mvc.Data.DTOs
{
    public class ValidationError
    {
        public string Driver { get; set; }
        public DateTime TripDate { get; set; }
        public string Message { get; set; }
    }
}
