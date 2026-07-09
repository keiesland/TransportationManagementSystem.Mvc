using TransportationManagementSystem.Models;

namespace TransportationManagementSystem.UtilityClasses
{
    public class TripGrouping
    {
        /// <summary>
        /// Which accumulator-fallback rules apply when adding a trip's times into
        /// the running lists for its group. These three patterns existed in the
        /// original code as an accident of its loop structure (different code paths
        /// happened to add data slightly differently depending on where you were in
        /// the iteration) rather than as a deliberate business rule. They're
        /// preserved exactly here for fidelity against verified production output —
        /// NOT because the distinction is necessarily "correct" business logic.
        /// Worth a follow-up conversation on whether to unify these once you trust
        /// the test suite enough to safely experiment.
        /// </summary>
        public enum AccumPattern
        {
            /// <summary>Normal mid-group trip. PickupArrival falls back to ScheduledPickup;
            /// ActualDropoff falls back to PickupArrival. Also the only pattern that
            /// triggers the auto-fill-from-ScheduledPickup quirk.</summary>
            MidGroup,

            /// <summary>This trip is the very last trip in the entire dataset, AND it
            /// continues the group already in progress. PickupArrival used raw (no
            /// fallback); ActualDropoff falls back to PickupArrival.</summary>
            LastTripContinuingGroup,

            /// <summary>This trip starts a brand-new group (different TripDateId than
            /// the trip before it). PickupArrival used raw; ActualDropoff falls back
            /// to ScheduledDropoff.</summary>
            NewGroupStart
        }

        public class GroupedTrip
        {
            public Trip Trip { get; init; }
            public AccumPattern Pattern { get; init; }
        }

        public class DriverDayGroup
        {
            public int DriverId { get; init; }
            public int TripDateId { get; init; }
            public int WeekNumber { get; init; }
            public List<GroupedTrip> Trips { get; init; }

            /// <summary>
            /// True if this group's WeeklyTime should be finalized/formatted —
            /// i.e. the next group (if any) belongs to a different driver or week,
            /// or this is the very last group overall.
            /// </summary>
            public bool ForceWeeklyReset { get; set; }
        }

        public static class TripGrouper
        {
            /// <summary>
            /// Sorts trips (driver, then date, then trip id — fixing the original
            /// OrderBy-without-reassignment bug) and groups them into one
            /// DriverDayGroup per contiguous (DriverId, TripDateId) run, tagging
            /// each trip with the accumulator pattern it needs.
            ///
            /// FIX: a group boundary requires BOTH DriverId and TripDateId to
            /// match the previous trip — not TripDateId alone. The original
            /// imperative code only compared TripDateId, which meant that if one
            /// driver's last trip happened to share a calendar date (TripDateId)
            /// with the next driver's first trip, their data would be silently
            /// merged into a single Summary. This is a narrow edge case (it only
            /// triggers when two drivers' date ranges happen to touch at exactly
            /// that boundary), but since a "day group" was always meant to mean
            /// one driver's one day, this fix has no ambiguity about intent.
            /// </summary>
            public static List<DriverDayGroup> GroupTrips(List<Trip> trips)
            {
                var sorted = trips
                    .OrderBy(t => t.DriverId)
                    .ThenBy(t => t.TripDate.Date)
                    .ThenBy(t => t.TripId)
                    .ToList();

                var groupedTrips = new List<GroupedTrip>();

                for (var i = 0; i < sorted.Count; i++)
                {
                    bool startsNewGroup = i > 0 &&
                        (sorted[i].TripDateId != sorted[i - 1].TripDateId ||
                         sorted[i].DriverId != sorted[i - 1].DriverId);

                    bool isGlobalLastTrip = i == sorted.Count - 1;

                    AccumPattern pattern;
                    if (startsNewGroup)
                    {
                        pattern = AccumPattern.NewGroupStart;
                    }
                    else if (isGlobalLastTrip)
                    {
                        pattern = AccumPattern.LastTripContinuingGroup;
                    }
                    else
                    {
                        pattern = AccumPattern.MidGroup;
                    }

                    groupedTrips.Add(new GroupedTrip { Trip = sorted[i], Pattern = pattern });
                }

                var groups = new List<DriverDayGroup>();
                DriverDayGroup current = null;

                foreach (var gt in groupedTrips)
                {
                    bool isNewGroup = current == null ||
                        gt.Trip.TripDateId != current.TripDateId ||
                        gt.Trip.DriverId != current.DriverId;

                    if (isNewGroup)
                    {
                        current = new DriverDayGroup
                        {
                            DriverId = gt.Trip.DriverId,
                            TripDateId = gt.Trip.TripDateId,
                            WeekNumber = gt.Trip.TripDate.WeekNumber,
                            Trips = new List<GroupedTrip>()
                        };
                        groups.Add(current);
                    }

                    current.Trips.Add(gt);
                }

                // Compute weekly-reset flag per group by looking at the next group.
                for (var g = 0; g < groups.Count; g++)
                {
                    var isLastGroup = g == groups.Count - 1;
                    if (isLastGroup)
                    {
                        groups[g].ForceWeeklyReset = true;
                        continue;
                    }

                    var next = groups[g + 1];
                    groups[g].ForceWeeklyReset =
                        groups[g].WeekNumber != next.WeekNumber || groups[g].DriverId != next.DriverId;
                }

                return groups;
            }
        }

    }
}

