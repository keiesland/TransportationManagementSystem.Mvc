using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.Entities;

namespace TransportationManagementSystem.Mvc.ViewModels
{
    public class TripListViewModel
    {
        public IEnumerable<Trip> Trips { get; set; }
        public TripDictionary CurrentRoute { get; set; }
        public int TotalPages { get; set; }

        public IEnumerable<Driver> Drivers { get; set; }
        public IEnumerable<TripDate> TripDates { get; set; }

        // data for pagesize drop-down - hardcoded
        public int[] PageSizes => new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    }

}
