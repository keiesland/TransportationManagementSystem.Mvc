using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Models;

namespace TransportationManagementSystem.ViewModels
{
    public class TripListViewModel
    {
        public IEnumerable<Trip> Trips { get; set; }
        public RideDictionary CurrentRoute { get; set; }
        public int TotalPages { get; set; }

        public IEnumerable<Driver> Drivers { get; set; }
        public IEnumerable<TripDate> TripDates { get; set; }

        // data for pagesize drop-down - hardcoded
        public int[] PageSizes => new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    }

}
