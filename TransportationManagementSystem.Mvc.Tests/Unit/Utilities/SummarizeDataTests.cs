
using TransportationManagementSystem.Models;
using TransportationManagementSystem.UtilityClasses;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class SummarizeDataTests
    {
        // ---------- Shared test data builders ----------

        private static Driver MakeDriver(int driverId, string lastName, string firstName, string fullName)
        {
            return new Driver
            {
                DriverId = driverId,
                LastName = lastName,
                FirstName = firstName,
                FullName = fullName
            };
        }

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber)
        {
            return new TripDate
            {
                TripDateId = tripDateId,
                Date = date,
                WeekNumber = weekNumber
            };
        }

        private static Trip MakeTrip(
            int tripId,
            int driverId,
            int tripDateId,
            TripDate tripDate,
            string tripActualStart,
            string scheduledPickup,
            string pickupArrival,
            string actualPickup,
            string actualDropoff,
            string scheduledDropoff,
            string tripActualEnd)
        {
            return new Trip
            {
                TripId = tripId,
                DriverId = driverId,
                TripDateId = tripDate.TripDateId,
                TripDate = tripDate, // CalculateSummaries reads this nav property directly
                TripActualStart = TimeSpan.Parse(tripActualStart),
                ScheduledPickup = TimeSpan.Parse(scheduledPickup),
                PickupArrival = TimeSpan.Parse(pickupArrival),
                ActualPickup = TimeSpan.Parse(actualPickup),
                ActualDropoff = TimeSpan.Parse(actualDropoff),
                ScheduledDropoff = TimeSpan.Parse(scheduledDropoff),
                TripActualEnd = TimeSpan.Parse(tripActualEnd)
            };
        }

        // ---------- Tests ----------

        [Fact]
        public void CalculateSummaries_SingleDriverSingleDayNoBreaks_ReturnsOneSummary()
        {
            // Arrange: one driver, one day, two back-to-back trips with no gap
            // large enough to count as a break (inOutList.Count should end up 0)
            var driver = MakeDriver(1, "Smith", "John", "Smith, John");
            var tripDate = MakeTripDate(100, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, 1, 
                    tripDate,
                    tripActualStart: "08:00:00",
                    scheduledPickup: "08:30:00",
                    pickupArrival: "08:25:00",
                    actualPickup: "08:30:00",
                    actualDropoff: "09:00:00",
                    scheduledDropoff: "09:00:00",
                    tripActualEnd: "17:00:00"),

                MakeTrip(2, 1, 2, 
                    tripDate,
                    tripActualStart: "08:00:00",
                    scheduledPickup: "09:15:00",
                    pickupArrival: "09:10:00",
                    actualPickup: "09:15:00",
                    actualDropoff: "10:00:00",
                    scheduledDropoff: "10:00:00",
                    tripActualEnd: "17:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            // Act
            var result = SummarizeData.CalculateSummaries(trips, drivers, dates);

            // Assert
            Assert.Single(result);
            var summary = result[0];
            Assert.Equal(driver.DriverId, summary.DriverId);
            Assert.Equal(tripDate.TripDateId, summary.TripDateId);

            // No breaks expected since the gap between dropoff (09:00) and the
            // next pickup (09:15) is well under the 1+ hour threshold
            Assert.Equal(TimeSpan.Zero, summary.Out1);
            Assert.Equal(TimeSpan.Zero, summary.In1);
        }

        [Fact]
        public void CalculateSummaries_SingleDriverSingleDayWithOneBreak_PopulatesOutInPair()
        {
            // Arrange: gap between first dropoff and second pickup is over an hour,
            // which should register as exactly one break (Out1/In1 populated)
            var driver = MakeDriver(2, "Doe", "Jane","Doe, Jane");
            var tripDate = MakeTripDate(200, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(10, 2, 3,
                    tripDate,
                    tripActualStart: "08:00:00",
                    scheduledPickup: "08:30:00",
                    pickupArrival: "08:25:00",
                    actualPickup: "08:30:00",
                    actualDropoff: "09:00:00",
                    scheduledDropoff: "09:00:00",
                    tripActualEnd: "17:00:00"),
 
                // Big gap here: dropoff at 09:00, next pickup at 11:00 (2-hour gap)
                MakeTrip(11, 2, 4,
                    tripDate,
                    tripActualStart: "08:00:00",
                    scheduledPickup: "11:00:00",
                    pickupArrival: "10:55:00",
                    actualPickup: "11:00:00",
                    actualDropoff: "12:00:00",
                    scheduledDropoff: "12:00:00",
                    tripActualEnd: "17:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            // Act
            var result = SummarizeData.CalculateSummaries(trips, drivers, dates);

            // Assert
            Assert.Single(result);
            var summary = result[0];

            // Exactly one break expected: Out1 should be the first dropoff time,
            // In1 should be 30 minutes before the later of pickupArrival/scheduledPickup
            Assert.Equal(TimeSpan.Parse("09:00:00"), summary.Out1);
            Assert.NotEqual(TimeSpan.Zero, summary.In1);

            // No second break expected
            Assert.Equal(TimeSpan.Zero, summary.Out2);
        }

        [Fact]
        public void CalculateSummaries_TripsAcrossTwoWeekNumbers_ResetsWeeklyTimeBetweenWeeks()
        {
            // Arrange: same driver, one trip in week 22 and one trip starting week 23.
            // WeeklyTime should reset when saveWeekNumber changes.
            var driver = MakeDriver(3, "Lee", "Pat","Lee, Pat");

            var dateWeek22 = MakeTripDate(300, new DateTime(2026, 5, 31), 22);
            var dateWeek23 = MakeTripDate(301, new DateTime(2026, 6, 1), 23);

            var trips = new List<Trip>
            {
                MakeTrip(20, 3, 300,  dateWeek22,
                    tripActualStart: "08:00:00", scheduledPickup: "08:30:00",
                    pickupArrival: "08:25:00", actualPickup: "08:30:00",
                    actualDropoff: "09:00:00", scheduledDropoff: "09:00:00",
                    tripActualEnd: "17:00:00"),

                MakeTrip(21, 3, 301, dateWeek23,
                    tripActualStart: "08:00:00", scheduledPickup: "08:30:00",
                    pickupArrival: "08:25:00", actualPickup: "08:30:00",
                    actualDropoff: "09:00:00", scheduledDropoff: "09:00:00",
                    tripActualEnd: "17:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { dateWeek22, dateWeek23 };

            // Act
            var result = SummarizeData.CalculateSummaries(trips, drivers, dates);

            // Assert: two separate day-summaries, each with its own WeeklyTime
            // string populated (not empty), since each is the last day in its week
            Assert.Equal(2, result.Count);
            Assert.All(result, summary => Assert.False(string.IsNullOrEmpty(summary.WeeklyTime)));
        }

        [Fact]
        public void CalculateSummaries_UnsortedInputTrips_ProducesSameResultAsSortedInput()
        {
            // Regression test for the OrderBy bug fix: feeding trips in scrambled
            // order should produce identical output to feeding them pre-sorted,
            // since CalculateSummaries now sorts internally.
            var driver = MakeDriver(4, "Park", "Sam","Park, Sam");
            var tripDate = MakeTripDate(400, new DateTime(2026, 6, 1), 22);

            var tripA = MakeTrip(30, 4, 400, 
                tripDate,
                "08:00:00", "08:30:00", "08:25:00", "08:30:00", "09:00:00", "09:00:00", "17:00:00");
            var tripB = MakeTrip(31, 4, 400, 
                tripDate,
                "08:00:00", "09:15:00", "09:10:00", "09:15:00", "10:00:00", "10:00:00", "17:00:00");

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            // Act: same trips, reversed order
            var sortedResult = SummarizeData.CalculateSummaries(
                new List<Trip> { tripA, tripB }, drivers, dates);

            var unsortedResult = SummarizeData.CalculateSummaries(
                new List<Trip> { tripB, tripA }, drivers, dates);

            // Assert
            Assert.Single(sortedResult);
            Assert.Single(unsortedResult);
            Assert.Equal(sortedResult[0].Start, unsortedResult[0].Start);
            Assert.Equal(sortedResult[0].End, unsortedResult[0].End);
            Assert.Equal(sortedResult[0].ActualTime, unsortedResult[0].ActualTime);
        }
    }
}
