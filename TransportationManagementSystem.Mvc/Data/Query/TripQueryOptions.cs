using DocumentFormat.OpenXml.Drawing.Charts;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Models;

namespace TransportationManagementSystem.Data.Query
{
    // extends generic QueryOptions<Trip> class to add a 
    // SortFilter() method that adds the Sort and Filter
    // code specific to the MobilityTransportation application
    public class TripQueryOptions : QueryOptions<Trip>
    {
        public void SortFilter(TripGridBuilder builder)
        {
            // filter
            if (builder.IsFilterByDriver)
            {
                Where = t => t.DriverId.ToString() == builder.CurrentRoute.DriverFilter;
            }

            if (builder.IsFilterByTripDate)
            {
                Where = t => t.TripDate.TripDateId.ToString() == builder.CurrentRoute.TripDateFilter;
            }


            if (builder.IsSortByDriver)
            {
                OrderBy = t => t.Driver.FirstName;
            }
            else if (builder.IsSortByTripDate)
            {
                OrderBy = t => t.TripDate.Date;
            }
            else
            {
                OrderBy = t => t.TripId;
            }
        }
    }

}
