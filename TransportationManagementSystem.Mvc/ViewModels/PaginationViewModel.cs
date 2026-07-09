using TransportationManagementSystem.Data.Grid;

namespace TransportationManagementSystem.ViewModels
{
    public class PaginationViewModel
    {
        public RideDictionary CurrentRoute { get; set; } = null!;
        public int TotalPages { get; set; }
        public int WindowSize { get; set; } = 2; // pages shown on each side of current
    }
}
