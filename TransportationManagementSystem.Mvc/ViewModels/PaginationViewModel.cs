using TransportationManagementSystem.Mvc.Data.Grid;

namespace TransportationManagementSystem.Mvc.ViewModels
{
    public class PaginationViewModel
    {
        public TripDictionary CurrentRoute { get; set; } = null!;
        public int TotalPages { get; set; }
        public int WindowSize { get; set; } = 2; // pages shown on each side of current
    }
}
