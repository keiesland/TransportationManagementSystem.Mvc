using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.UnitOfWork;

namespace TransportationManagementSystem.Mvc.Utilities
{
    public static class SummarizeData
    {
        public static async Task SummarizeDriverDataAsync(
            TripUnitOfWork data,
            List<Trip> trips,
            List<Driver> drivers,
            List<TripDate> dates,
            CancellationToken ct = default)
        {
            var summaries = CalculateSummaries(trips, drivers, dates);

            if (summaries.Count > 0)
            {
                await data.Summaries.BulkInsertAsync(summaries, ct);
            }
        }

        public static List<Summary> CalculateSummaries(
            List<Trip> trips,
            List<Driver> drivers,
            List<TripDate> dates)
        {
            var groups = DriverDayGrouper.GroupTrips(trips, drivers);
            var summaries = new List<Summary>();
            var weeklyTime = TimeSpan.Zero;

            foreach (var group in groups)
            {
                summaries.Add(SummaryBuilder.Build(group, dates, ref weeklyTime));
            }

            return summaries;
        }
    }
}
