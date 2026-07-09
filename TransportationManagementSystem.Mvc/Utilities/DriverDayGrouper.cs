using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Entities;

namespace TransportationManagementSystem.Mvc.Utilities
{
    public class DriverDayGroup
    {
        public int DriverId { get; init; }
        public int TripDateId { get; init; }
        public DriverDay DriverDay { get; init; }
        public bool ForceWeeklyReset { get; set; }
    }

    public static class DriverDayGrouper
    {
        public static List<DriverDayGroup> GroupTrips(List<Trip> trips, List<Driver> drivers)
        {
            var sorted = trips
                .OrderBy(t => t.DriverId)
                .ThenBy(t => t.TripDate.Date)   // real calendar date, not TripDateId
                .ThenBy(t => t.TripId)
                .ToList();

            var groups = new List<DriverDayGroup>();
            DriverDayGroup current = null;
            int currentDriverId = -1, currentTripDateId = -1;

            foreach (var trip in sorted)
            {
                bool isNewGroup = current == null ||
                    trip.DriverId != currentDriverId ||
                    trip.TripDateId != currentTripDateId;

                if (isNewGroup)
                {
                    var driver = drivers.Find(d => d.DriverId == trip.DriverId);

                    current = new DriverDayGroup
                    {
                        DriverId = trip.DriverId,
                        TripDateId = trip.TripDateId,
                        DriverDay = new DriverDay
                        {
                            Driver = driver?.FullName,
                            TripDate = trip.TripDate.Date,
                            WeekNumber = trip.TripDate.WeekNumber,
                            TripActualStart = trip.TripActualStart,
                            TripActualEnd = trip.TripActualEnd
                        }
                    };

                    currentDriverId = trip.DriverId;
                    currentTripDateId = trip.TripDateId;
                    groups.Add(current);
                }

                current.DriverDay.Trips.Add(new TripSegment
                {
                    ScheduledPickupTime = trip.ScheduledPickup,
                    PickupArrivalTime = trip.PickupArrival,
                    ActualPickupTime = trip.ActualPickup,
                    ActualDropoffTime = trip.ActualDropoff,
                    ScheduledDropoffTime = trip.ScheduledDropoff,
                    IsNoShow = trip.ActualPickup == TimeSpan.Zero && trip.ActualDropoff == TimeSpan.Zero
                });
            }

            // Weekly-reset flag, same logic as before -- last group overall, or
            // next group belongs to a different driver/week.
            for (var g = 0; g < groups.Count; g++)
            {
                if (g == groups.Count - 1)
                {
                    groups[g].ForceWeeklyReset = true;
                    continue;
                }

                var next = groups[g + 1];
                groups[g].ForceWeeklyReset =
                    groups[g].DriverDay.WeekNumber != next.DriverDay.WeekNumber ||
                    groups[g].DriverId != next.DriverId;
            }

            return groups;
        }
    }
}
