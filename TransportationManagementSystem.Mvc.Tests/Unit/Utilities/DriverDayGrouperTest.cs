using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Utilities;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Utilities
{
    public class DriverDayGrouperTests
    {
        private static Driver MakeDriver(int driverId, string fullName) =>
            new Driver { DriverId = driverId, FullName = fullName };

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber) =>
            new TripDate { TripDateId = tripDateId, Date = date, WeekNumber = weekNumber };

        private static Trip MakeTrip(
            int tripId, int driverId, TripDate tripDate,
            string actualStart = "06:00:00", string scheduledPickup = "06:30:00",
            string pickupArrival = "06:25:00", string actualPickup = "06:30:00",
            string actualDropoff = "07:00:00", string scheduledDropoff = "07:00:00",
            string actualEnd = "18:00:00")
        {
            return new Trip
            {
                TripId = tripId,
                DriverId = driverId,
                TripDateId = tripDate.TripDateId,
                TripDate = tripDate,
                TripActualStart = TimeSpan.Parse(actualStart),
                ScheduledPickup = TimeSpan.Parse(scheduledPickup),
                PickupArrival = TimeSpan.Parse(pickupArrival),
                ActualPickup = TimeSpan.Parse(actualPickup),
                ActualDropoff = TimeSpan.Parse(actualDropoff),
                ScheduledDropoff = TimeSpan.Parse(scheduledDropoff),
                TripActualEnd = TimeSpan.Parse(actualEnd)
            };
        }

        // ===== Grouping correctness =====

        [Fact]
        public void GroupTrips_SameDriverAndDate_GroupsIntoOneDriverDay()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var tripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, actualPickup: "06:30:00", actualDropoff: "07:00:00"),
                MakeTrip(2, 1, tripDate, actualPickup: "09:00:00", actualDropoff: "09:30:00")
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.Single(groups);
            Assert.Equal(2, groups[0].DriverDay.Trips.Count);
        }

        [Fact]
        public void GroupTrips_DifferentDriversSharingSameTripDateId_DoesNotMergeGroups()
        {
            // Regression guard: the original TripGrouper bug only compared
            // TripDateId, not DriverId+TripDateId, letting two different
            // drivers' data silently merge into one Summary when their date
            // ranges happened to touch. This confirms the fix holds.
            var driverA = MakeDriver(1, "Bango, Stephen");
            var driverB = MakeDriver(2, "King, Robert");
            var sharedTripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, sharedTripDate),
                MakeTrip(2, 2, sharedTripDate)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driverA, driverB });

            Assert.Equal(2, groups.Count);
            Assert.NotEqual(groups[0].DriverId, groups[1].DriverId);
        }

        [Fact]
        public void GroupTrips_SameDriverDifferentDates_CreatesSeparateGroups()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var day1 = MakeTripDate(100, new DateTime(2024, 9, 23), 39);
            var day2 = MakeTripDate(101, new DateTime(2024, 9, 24), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, day1),
                MakeTrip(2, 1, day2)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.Equal(2, groups.Count);
        }

        // ===== Sorting correctness =====

        [Fact]
        public void GroupTrips_SortsByCalendarDate_NotByTripDateId()
        {
            // Regression guard: the original bug sorted by TripDateId (a
            // surrogate FK, assigned by insertion order) instead of the real
            // calendar date. Here, TripDateId is deliberately assigned in
            // the OPPOSITE order of the real dates -- if grouping ever
            // reverts to sorting by TripDateId, this test will produce
            // groups in the wrong (9/24, 9/23) order and fail.
            var driver = MakeDriver(1, "Wade, Donald");
            var laterDateWithLowerId = MakeTripDate(1, new DateTime(2024, 9, 24), 39);
            var earlierDateWithHigherId = MakeTripDate(2, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, laterDateWithLowerId),
                MakeTrip(2, 1, earlierDateWithHigherId)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.Equal(2, groups.Count);
            Assert.Equal(new DateTime(2024, 9, 23), groups[0].DriverDay.TripDate);
            Assert.Equal(new DateTime(2024, 9, 24), groups[1].DriverDay.TripDate);
        }

        [Fact]
        public void GroupTrips_SortsByDriverIdFirst()
        {
            var driverA = MakeDriver(2, "Wilson, Tomika");
            var driverB = MakeDriver(1, "Bango, Stephen");
            var tripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 2, tripDate), // driver 2 appears first in the list
                MakeTrip(2, 1, tripDate)  // driver 1 appears second
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driverA, driverB });

            Assert.Equal(1, groups[0].DriverId); // but driver 1 should sort first
            Assert.Equal(2, groups[1].DriverId);
        }

        // ===== DriverDay field mapping =====

        [Fact]
        public void GroupTrips_PopulatesDriverDayFields_FromFirstTripInGroup()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var tripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, actualStart: "12:30:00", actualEnd: "15:46:00")
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            var day = groups[0].DriverDay;
            Assert.Equal("Bango, Stephen", day.Driver);
            Assert.Equal(new DateTime(2024, 9, 23), day.TripDate);
            Assert.Equal(39, day.WeekNumber);
            Assert.Equal(TimeSpan.Parse("12:30:00"), day.TripActualStart);
            Assert.Equal(TimeSpan.Parse("15:46:00"), day.TripActualEnd);
        }

        [Fact]
        public void GroupTrips_MapsTripSegmentFieldsCorrectly()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var tripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate,
                    scheduledPickup: "12:30:00", pickupArrival: "12:34:00",
                    actualPickup: "12:41:00", actualDropoff: "13:07:00",
                    scheduledDropoff: "12:55:00")
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            var segment = groups[0].DriverDay.Trips[0];
            Assert.Equal(TimeSpan.Parse("12:30:00"), segment.ScheduledPickupTime);
            Assert.Equal(TimeSpan.Parse("12:34:00"), segment.PickupArrivalTime);
            Assert.Equal(TimeSpan.Parse("12:41:00"), segment.ActualPickupTime);
            Assert.Equal(TimeSpan.Parse("13:07:00"), segment.ActualDropoffTime);
            Assert.Equal(TimeSpan.Parse("12:55:00"), segment.ScheduledDropoffTime);
        }

        // ===== No-show detection =====

        [Fact]
        public void GroupTrips_ZeroActualPickupAndDropoff_MarksTripAsNoShow()
        {
            var driver = MakeDriver(1, "Wade, Donald");
            var tripDate = MakeTripDate(100, new DateTime(2024, 10, 4), 40);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, actualPickup: "00:00:00", actualDropoff: "00:00:00")
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.True(groups[0].DriverDay.Trips[0].IsNoShow);
        }

        [Fact]
        public void GroupTrips_NonZeroPickupOrDropoff_IsNotMarkedNoShow()
        {
            var driver = MakeDriver(1, "Wade, Donald");
            var tripDate = MakeTripDate(100, new DateTime(2024, 10, 4), 40);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, actualPickup: "12:41:00", actualDropoff: "13:07:00")
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.False(groups[0].DriverDay.Trips[0].IsNoShow);
        }

        // ===== ForceWeeklyReset =====

        [Fact]
        public void GroupTrips_LastGroupOverall_ForceWeeklyResetIsTrue()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var tripDate = MakeTripDate(100, new DateTime(2024, 9, 23), 39);

            var trips = new List<Trip> { MakeTrip(1, 1, tripDate) };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.True(groups[^1].ForceWeeklyReset);
        }

        [Fact]
        public void GroupTrips_NextGroupDifferentDriver_ForceWeeklyResetIsTrue()
        {
            var driverA = MakeDriver(1, "Bango, Stephen");
            var driverB = MakeDriver(2, "King, Robert");
            var day1 = MakeTripDate(100, new DateTime(2024, 9, 23), 39);
            var day2 = MakeTripDate(101, new DateTime(2024, 9, 24), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, day1),
                MakeTrip(2, 2, day2)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driverA, driverB });

            Assert.True(groups[0].ForceWeeklyReset);  // driver 1's only day -> next group is a different driver
        }

        [Fact]
        public void GroupTrips_NextGroupDifferentWeek_SameDriver_ForceWeeklyResetIsTrue()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var weekA = MakeTripDate(100, new DateTime(2024, 9, 27), 39);
            var weekB = MakeTripDate(101, new DateTime(2024, 9, 30), 40);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, weekA),
                MakeTrip(2, 1, weekB)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.True(groups[0].ForceWeeklyReset);
            Assert.True(groups[1].ForceWeeklyReset); // also last group overall
        }

        [Fact]
        public void GroupTrips_NextGroupSameDriverSameWeek_ForceWeeklyResetIsFalse()
        {
            var driver = MakeDriver(1, "Bango, Stephen");
            var day1 = MakeTripDate(100, new DateTime(2024, 9, 23), 39);
            var day2 = MakeTripDate(101, new DateTime(2024, 9, 24), 39);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, day1),
                MakeTrip(2, 1, day2)
            };

            var groups = DriverDayGrouper.GroupTrips(trips, new List<Driver> { driver });

            Assert.False(groups[0].ForceWeeklyReset); // same driver, same week, more days coming
            Assert.True(groups[1].ForceWeeklyReset);  // last group overall
        }
    }
}