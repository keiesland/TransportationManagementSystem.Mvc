using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.Data.Query;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Repositories.Interfaces;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.UnitOfWork;
using TransportationManagementSystem.Mvc.Utilities;
using TransportationManagementSystem.Mvc.ViewModels;

namespace TransportationManagementSystem.Mvc.Services
{

    public class TripService : ITripService
    {
        private readonly TripUnitOfWork _data;


        public TripService(TripContext ctx, IBulkOperationsProvider bulkOps = null)
        {
            _data = new TripUnitOfWork(ctx, bulkOps);
        }

        /// <summary>
        /// Retrieves a paginated, sorted, and filtered list of trips for the List view
        /// </summary>
        public TripListViewModel GetTripsForList(TripGridDTO values, ISession session)
        {
            // Get grid builder, which loads route segment values and stores them in session
            var builder = new TripGridBuilder(session, values,
                defaultSortField: nameof(Trip.Driver.FullName));

            // Build query options with includes, sorting, and paging
            var options = BuildTripQueryOptions(builder);

            // Create view model with trips, dropdown data, and paging info
            var vm = new TripListViewModel
            {
                Trips = _data.Trips.List(options),
                Drivers = GetDriversForDropdown(),
                TripDates = GetTripDatesForDropdown(),
                CurrentRoute = builder.CurrentRoute,
                TotalPages = builder.GetTotalPages(_data.Trips.Count(options))
            };

            return vm;
        }

        /// <summary>
        /// Retrieves a single trip with related data for the Details view
        /// </summary>
        public Trip GetTripDetails(int id)
        {
            var trip = _data.Trips.Get(new QueryOptions<Trip>
            {
                Includes = "Driver, TripDate",
                Where = b => b.TripId == id
            });

            return trip;
        }

        /// <summary>
        /// Applies or clears filter criteria and saves to session
        /// </summary>
        public TripDictionary ApplyFilter(string[] filter, bool clear, ISession session)
        {
            var builder = new TripGridBuilder(session);

            if (clear)
            {
                builder.ClearFilterSegments();
            }
            else
            {
                builder.LoadFilterSegments(filter);
            }

            builder.SaveRouteSegments();

            return builder.CurrentRoute;
        }

        /// <summary>
        /// Updates page size preference and saves to session
        /// </summary>
        public void UpdatePageSize(int pageSize, ISession session)
        {
            var builder = new TripGridBuilder(session);
            builder.CurrentRoute.PageSize = pageSize;
            builder.SaveRouteSegments();
        }

        /// <summary>
        /// Deletes all data across every table (Trips, Summaries, Drivers, TripDates)
        /// </summary>
        public async Task ClearAllDataAsync(CancellationToken ct)
        {
            await DeleteRecords.DeleteAllTableDataAsync(_data, ct);
        }

        // ========== Private Helper Methods ==========

        private TripQueryOptions BuildTripQueryOptions(TripGridBuilder builder)
        {
            var options = new TripQueryOptions
            {
                Includes = "Driver, TripDate",
                OrderByDirection = builder.CurrentRoute.SortDirection,
                PageNumber = builder.CurrentRoute.PageNumber,
                PageSize = builder.CurrentRoute.PageSize
            };

            options.SortFilter(builder);

            return options;
        }

        private List<Driver> GetDriversForDropdown()
        {
            return _data.Drivers.List(new QueryOptions<Driver>
            {
                OrderBy = d => d.DriverId
            });
        }

        private List<TripDate> GetTripDatesForDropdown()
        {
            return _data.TripDates.List(new QueryOptions<TripDate>
            {
                OrderBy = t => t.Date
            });
        }
    }

}
