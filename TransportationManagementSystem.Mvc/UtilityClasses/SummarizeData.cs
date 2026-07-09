using TransportationManagementSystem.Models;
using TransportationManagementSystem.UnitOfWork;
using TransportationManagementSystem.UtilityClasses;
using static TransportationManagementSystem.UtilityClasses.TripGrouping;

public static class SummarizeData
{
    /// <summary>
    /// Calculates driver day/week summaries from raw trip data and persists
    /// them. Thin wrapper: all calculation logic lives in CalculateSummaries
    /// (pure, testable) and its underlying modules.
    /// </summary>
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

    /// <summary>
    /// Pure calculation: given trips/drivers/dates, returns the list of
    /// Summary records to persist. No database access — fully unit
    /// testable with in-memory lists and no mocking required.
    ///
    /// This method itself is now just orchestration. All the actual logic
    /// lives in, and can be tested independently via:
    ///   - TripGrouper.GroupTrips      (sorting + grouping + pattern assignment)
    ///   - AccumulatorBuilder.Build    (per-group time-list building)
    ///   - BreakDetector.DetectBreaks  (in/out break-pairing)
    ///   - TimeCalculator              (start/end time, weekly-time formatting)
    ///   - SummaryBuilder.Build        (assembling one Summary)
    /// </summary>
    public static List<Summary> CalculateSummaries(
        List<Trip> trips,
        List<Driver> drivers,
        List<TripDate> dates)
    {
        var groups = TripGrouper.GroupTrips(trips);
        var summaries = new List<Summary>();
        var weeklyTime = TimeSpan.Zero;

        foreach (var group in groups)
        {
            var acc = AccumulatorBuilder.Build(group);
            var summary = SummaryBuilder.Build(group, acc, drivers, dates, ref weeklyTime);
            summaries.Add(summary);
        }

        return summaries;
    }
}
