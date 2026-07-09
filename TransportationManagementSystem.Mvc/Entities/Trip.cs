using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportationManagementSystem.Mvc.Entities
{
    public class Trip
    {
        [Key]
        public int TripId { get; set; }

        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        // navigation property
        public Driver Driver { get; set; }

        [ForeignKey("TripDate")]
        public int TripDateId { get; set; }

        // navigation property
        public TripDate TripDate { get; set; }
        
        [Required(ErrorMessage = "Please enter the trip actual start.")]
        public TimeSpan TripActualStart { get; set; }

        [Required(ErrorMessage = "Please enter the scheduled pickup time.")]
        public TimeSpan ScheduledPickup { get; set; }

        [Required(ErrorMessage = "Please enter the pickup arrival time.")]
        public TimeSpan PickupArrival { get; set; }

        [Required(ErrorMessage = "Please enter the actual pickup time.")]
        public TimeSpan ActualPickup { get; set; }

        [Required(ErrorMessage = "Please enter the actual dropoff time.")]
        public TimeSpan ActualDropoff { get; set; }

        [Required(ErrorMessage = "Please enter the scheduled dropoff time.")]
        public TimeSpan ScheduledDropoff { get; set; }

        [Required(ErrorMessage = "Please enter the trip actual end.")]
        public TimeSpan TripActualEnd { get; set; }
    }
}
