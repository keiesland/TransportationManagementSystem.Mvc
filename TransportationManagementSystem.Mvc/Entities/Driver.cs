using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransportationManagementSystem.Mvc.Entities
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        
        public string LastName { get; set; }

        
        public string FirstName { get; set; }

       
        public string FullName { get; set; }

        public ICollection<Trip> Trips { get; set; }

    }
}
