using System.ComponentModel.DataAnnotations;

namespace TransportationManagementSystem.Models
{
    public class TripDate
    {
        [Key]
        public int TripDateId { get; set; }

        public DateTime Date { get; set; }

        public int WeekNumber { get; set; }

    }
}
