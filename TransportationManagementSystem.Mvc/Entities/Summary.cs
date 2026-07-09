using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportationManagementSystem.Mvc.Entities
{
    public class Summary
    {
        [Key]
        public int SummaryId { get; set; }

        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        // navigation property
        public Driver Driver { get; set; }


        [ForeignKey("TripDate")]
        public int TripDateId { get; set; }

        // navigation property
        public TripDate TripDate { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan Out1 { get; set; }

        public TimeSpan In1 { get; set; }

        public TimeSpan Out2 { get; set; }

        public TimeSpan In2 { get; set; }

        public TimeSpan Out3 { get; set; }

        public TimeSpan In3 { get; set; }

        public TimeSpan Out4 { get; set; }

        public TimeSpan In4 { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan ActualTime { get; set; }

        public string WeeklyTime { get; set; }

    }
}
