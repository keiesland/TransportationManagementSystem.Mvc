using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.Entities;

namespace TransportationManagementSystem.Mvc.Data.Query
{
    public class SummaryQueryOptions : QueryOptions<Summary>
    {
        public void SortFilter(SummaryGridBuilder builder)
        {
            // filter
            if (builder.IsFilterByDriver)
            {
                Where = s => s.DriverId.ToString() == builder.CurrentRoute.DriverFilter;
            }

            if (builder.IsFilterByTripDate)
            {
                Where = s => s.TripDate.TripDateId.ToString() == builder.CurrentRoute.TripDateFilter;
            }


            if (builder.IsSortByDriver)
            {
                OrderBy = s => s.Driver.FirstName;
            }
            else if (builder.IsSortByTripDate)
            {
                OrderBy = s => s.TripDate.Date;
            }
            else
            {
                OrderBy = s => s.SummaryId;
            }
        }
    }

}
