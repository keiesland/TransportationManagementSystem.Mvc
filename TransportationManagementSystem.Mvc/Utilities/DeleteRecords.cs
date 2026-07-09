using TransportationManagementSystem.Mvc.UnitOfWork;

namespace TransportationManagementSystem.Mvc.Utilities
{
    /// <summary>
    /// Now routes through the UnitOfWork's repositories (which expose BulkDeleteAllAsync)
    /// instead of querying the raw TripContext directly.
    /// </summary>
    public static class DeleteRecords
    {
        public static Task DeleteAllTripsAsync(ITripUnitOfWork data, CancellationToken ct = default)
            => data.Trips.BulkDeleteAllAsync(ct);

        public static Task DeleteAllSummariesAsync(ITripUnitOfWork data, CancellationToken ct = default)
            => data.Summaries.BulkDeleteAllAsync(ct);

        public static Task DeleteAllTripDatesAsync(ITripUnitOfWork data, CancellationToken ct = default)
            => data.TripDates.BulkDeleteAllAsync(ct);

        public static Task DeleteAllDriversAsync(ITripUnitOfWork data, CancellationToken ct = default)
            => data.Drivers.BulkDeleteAllAsync(ct);

        public static async Task DeleteAllTableDataAsync(ITripUnitOfWork data, CancellationToken ct = default)
        {
            // BulkDeleteAllAsync already checks AnyAsync internally, so no need to check here
            await DeleteAllTripsAsync(data, ct);
            await DeleteAllSummariesAsync(data, ct);
            await DeleteAllDriversAsync(data, ct);
            await DeleteAllTripDatesAsync(data, ct);
        }
    }
}
