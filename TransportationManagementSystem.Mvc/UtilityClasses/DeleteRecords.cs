using EFCore.BulkExtensions;
using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Repositories.Interfaces;
using TransportationManagementSystem.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace TransportationManagementSystem.UtilityClasses
{
    /// <summary>
    /// Now routes through the UnitOfWork's repositories (which expose BulkDeleteAllAsync)
    /// instead of querying the raw TripContext directly.
    /// </summary>
    public static class DeleteRecords
    {
        public static Task DeleteAllTripsAsync(TripUnitOfWork data, CancellationToken ct = default)
            => data.Trips.BulkDeleteAllAsync(ct);

        public static Task DeleteAllSummariesAsync(TripUnitOfWork data, CancellationToken ct = default)
            => data.Summaries.BulkDeleteAllAsync(ct);

        public static Task DeleteAllTripDatesAsync(TripUnitOfWork data, CancellationToken ct = default)
            => data.TripDates.BulkDeleteAllAsync(ct);

        public static Task DeleteAllDriversAsync(TripUnitOfWork data, CancellationToken ct = default)
            => data.Drivers.BulkDeleteAllAsync(ct);

        public static async Task DeleteAllTableDataAsync(TripUnitOfWork data, CancellationToken ct = default)
        {
            // BulkDeleteAllAsync already checks AnyAsync internally, so no need to check here
            await DeleteAllTripsAsync(data, ct);
            await DeleteAllSummariesAsync(data, ct);
            await DeleteAllDriversAsync(data, ct);
            await DeleteAllTripDatesAsync(data, ct);
        }
    }
}
