using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Services
{
    public class AggregationService : IAggregationService
    {
        public List<DriverDay> Aggregate(List<TripImportRow> rows)
        {
            var groups = rows
                .GroupBy(r => new { r.Driver, r.TripDate })
                .Select(g => BuildDriverDay(g.Key.Driver, g.Key.TripDate, g.ToList()))
                .OrderBy(d => d.Driver)
                .ThenBy(d => d.TripDate)
                .ToList();

            return groups;
        }

        private static DriverDay BuildDriverDay(string driver, DateTime rideDate, List<TripImportRow> rows)
        {
            var first = rows[0];

            var day = new DriverDay
            {
                Driver = driver,
                TripDate = rideDate,
                TripActualStart = first.TripActualStartTime,   // identical across all rows for this group
                TripActualEnd = first.TripActualEndTime,
                WeekNumber = first.WeekNumber
            };

            foreach (var row in rows)
            {
                day.Trips.Add(new TripSegment
                {
                    ScheduledPickupTime = row.ScheduledPickupTime,
                    PickupArrivalTime = row.PickupArrivalTime,
                    ActualPickupTime = row.ActualPickupTime,
                    ActualDropoffTime = row.ActualDropoffTime,
                    ScheduledDropoffTime = row.ScheduledDropoffTime,
                    IsNoShow = IsNoShowPattern(row)
                });
            }

            return day;
        }

        private static bool IsNoShowPattern(TripImportRow row) =>
            row.ActualPickupTime == TimeSpan.Zero && row.ActualDropoffTime == TimeSpan.Zero;
    }
}
