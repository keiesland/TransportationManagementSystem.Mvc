using TransportationManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static TransportationManagementSystem.UtilityClasses.TripGrouping;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class TripGrouperTests
    {
        // ---------- Shared builders ----------

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber)
        {
            return new TripDate { TripDateId = tripDateId, Date = date, WeekNumber = weekNumber };
        }

        private static Trip MakeTrip(int tripId, int driverId, TripDate tripDate)
        {
            return new Trip
            {
                TripId = tripId,
                DriverId = driverId,
                TripDateId = tripDate.TripDateId,
                TripDate = tripDate,
                TripActualStart = TimeSpan.FromHours(8),
                ScheduledPickup = TimeSpan.FromHours(8.5),
                PickupArrival = TimeSpan.FromHours(8.4),
                ActualPickup = TimeSpan.FromHours(8.5),
                ActualDropoff = TimeSpan.FromHours(9),
                ScheduledDropoff = TimeSpan.FromHours(9),
                TripActualEnd = TimeSpan.FromHours(17)
            };
        }

        // ---------- Sorting ----------

        [Fact]
        public void GroupTrips_UnsortedInput_GroupsCorrectlyRegardlessOfInputOrder()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 2), 22);

            // Deliberately scrambled order, and driver 2's trip listed before driver 1's
            var trips = new List<Trip>
            {
                MakeTrip(10, 2, date2),
                MakeTrip(11, 1, date1),
                MakeTrip(12, 1, date2),
            };

            var groups = TripGrouper.GroupTrips(trips);

            // Driver 1 has two day-groups (date1, date2); driver 2 has one (date2)
            Assert.Equal(3, groups.Count);
            Assert.Equal(1, groups[0].DriverId);
            Assert.Equal(1, groups[0].TripDateId);
            Assert.Equal(1, groups[1].DriverId);
            Assert.Equal(2, groups[1].TripDateId);
            Assert.Equal(2, groups[2].DriverId);
            Assert.Equal(2, groups[2].TripDateId);
        }

        // ---------- Pattern assignment ----------

        [Fact]
        public void GroupTrips_FirstTripOverall_GetsMidGroupPattern_WhenMoreTripsFollowInSameGroup()
        {
            var date = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var trips = new List<Trip> { MakeTrip(1, 1, date), MakeTrip(2, 1, date) };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.Equal(AccumPattern.MidGroup, groups[0].Trips[0].Pattern);
        }

        [Fact]
        public void GroupTrips_SoleTripInDataset_GetsLastTripContinuingGroupPattern()
        {
            var date = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var trips = new List<Trip> { MakeTrip(1, 1, date) };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.Equal(AccumPattern.LastTripContinuingGroup, groups[0].Trips[0].Pattern);
        }

        [Fact]
        public void GroupTrips_LastTripContinuingSameGroup_GetsLastTripContinuingGroupPattern()
        {
            var date = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var trips = new List<Trip> { MakeTrip(1, 1, date), MakeTrip(2, 1, date) };

            var groups = TripGrouper.GroupTrips(trips);

            // Second trip is both the global last trip AND continues the same group
            Assert.Equal(AccumPattern.LastTripContinuingGroup, groups[0].Trips[1].Pattern);
        }

        [Fact]
        public void GroupTrips_TripStartingMidListNewGroup_GetsNewGroupStartPattern()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 2), 22);
            var date3 = MakeTripDate(3, new DateTime(2026, 6, 3), 22);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1),
                MakeTrip(2, 1, date2), // starts a new group, NOT the last trip overall
                MakeTrip(3, 1, date3)
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.Equal(AccumPattern.NewGroupStart, groups[1].Trips[0].Pattern);
        }

        [Fact]
        public void GroupTrips_RegressionForBugFix_LastTripStartingNewGroup_GetsNewGroupStartNotLastTripPattern()
        {
            // This is exactly the scenario that exposed the original bug: the very
            // last trip in the dataset is also the first (and only) trip of a
            // brand-new group. It must get NewGroupStart, NOT
            // LastTripContinuingGroup — otherwise its data gets folded into the
            // wrong group's accumulators.
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 2), 23);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1),
                MakeTrip(2, 1, date2) // last trip overall, also starts a new group
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.Equal(2, groups.Count);
            Assert.Equal(AccumPattern.NewGroupStart, groups[1].Trips[0].Pattern);
        }

        [Fact]
        public void GroupTrips_RegressionForBugFix_TwoDriversSharingABoundaryDate_StayInSeparateGroups()
        {
            // Regression test for the cross-driver grouping fix: if one driver's
            // last trip and the next driver's first trip happen to share the same
            // TripDateId, they must NOT be merged into a single group just
            // because the calendar date matches.
            var sharedDate = MakeTripDate(99, new DateTime(2026, 6, 5), 23);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, sharedDate), // Driver 1's trip on the shared date
                MakeTrip(2, 2, sharedDate)  // Driver 2's trip on the SAME date
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.Equal(2, groups.Count);
            Assert.Equal(1, groups[0].DriverId);
            Assert.Equal(2, groups[1].DriverId);
        }

        // ---------- ForceWeeklyReset ----------

        [Fact]
        public void GroupTrips_LastGroupOverall_AlwaysForcesWeeklyReset()
        {
            var date = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var trips = new List<Trip> { MakeTrip(1, 1, date) };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.True(groups[^1].ForceWeeklyReset);
        }

        [Fact]
        public void GroupTrips_NextGroupSameDriverSameWeek_DoesNotForceWeeklyReset()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 2), 22); // same week, same driver

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1),
                MakeTrip(2, 1, date2)
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.False(groups[0].ForceWeeklyReset);
        }

        [Fact]
        public void GroupTrips_NextGroupDifferentWeek_ForcesWeeklyResetOnEarlierGroup()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 5, 31), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 1), 23); // new week

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1),
                MakeTrip(2, 1, date2)
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.True(groups[0].ForceWeeklyReset);
        }

        [Fact]
        public void GroupTrips_NextGroupDifferentDriver_ForcesWeeklyResetOnEarlierGroup()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 1), 22); // same week, different driver

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1),
                MakeTrip(2, 2, date2)
            };

            var groups = TripGrouper.GroupTrips(trips);

            Assert.True(groups[0].ForceWeeklyReset);
        }
    }


}
