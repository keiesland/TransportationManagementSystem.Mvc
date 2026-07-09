using TransportationManagementSystem.Models;
using TransportationManagementSystem.Services;
using TransportationManagementSystem.UtilityClasses;
using System;
using System.Collections.Generic;
using System.Text;
using static TransportationManagementSystem.UtilityClasses.TripGrouping;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class SummaryBuilderTests
    {
        // ---------- Shared builders (matching SummarizeDataTests conventions) ----------

        private static Driver MakeDriver(int driverId, string fullName)
        {
            return new Driver { DriverId = driverId, FullName = fullName };
        }

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber)
        {
            return new TripDate { TripDateId = tripDateId, Date = date, WeekNumber = weekNumber };
        }

        private static Trip MakeTrip(
            int tripId,
            int driverId,
            TripDate tripDate,
            string actualStart,
            string scheduledPickup,
            string pickupArrival,
            string actualPickup,
            string actualDropoff,
            string scheduledDropoff,
            string actualEnd)
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

        [Fact]
        public void Build_FourBreaksInOneDay_PopulatesAllEightOutInFields()
        {
            // Arrange: 5 trips, each separated by a 2-hour gap (well over the
            // 1-hour break threshold), giving exactly 4 detected breaks —
            // enough to hit every case (0 through 7) in ApplyBreaks' switch.
            var driver = MakeDriver(1, "Lee, Pat");
            var tripDate = MakeTripDate(500, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, "06:00:00", "06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00", "20:00:00"),
                MakeTrip(2, 1, tripDate, "06:00:00", "09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00", "20:00:00"),
                MakeTrip(3, 1, tripDate, "06:00:00", "11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00", "20:00:00"),
                MakeTrip(4, 1, tripDate, "06:00:00", "14:00:00", "13:55:00", "14:00:00", "14:30:00", "14:30:00", "20:00:00"),
                MakeTrip(5, 1, tripDate, "06:00:00", "16:30:00", "16:25:00", "16:30:00", "17:00:00", "17:00:00", "20:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            var groups = TripGrouper.GroupTrips(trips);
            Assert.Single(groups); // sanity check: all 5 trips are one driver/day group

            var acc = AccumulatorBuilder.Build(groups[0]);
            var weeklyTime = TimeSpan.Zero;

            // Act
            var summary = SummaryBuilder.Build(groups[0], acc, drivers, dates, ref weeklyTime);

            // Assert: all 8 fields populated (4 break pairs), confirming every
            // case in ApplyBreaks' switch (0 through 7) actually ran.
            Assert.NotEqual(TimeSpan.Zero, summary.Out1);
            Assert.NotEqual(TimeSpan.Zero, summary.In1);
            Assert.NotEqual(TimeSpan.Zero, summary.Out2);
            Assert.NotEqual(TimeSpan.Zero, summary.In2);
            Assert.NotEqual(TimeSpan.Zero, summary.Out3);
            Assert.NotEqual(TimeSpan.Zero, summary.In3);
            Assert.NotEqual(TimeSpan.Zero, summary.Out4);
            Assert.NotEqual(TimeSpan.Zero, summary.In4);

            // And the specific values are exactly the dropoff times (Out) and
            // 30-min-before-pickup times (In) we'd expect from the gaps above.
            Assert.Equal(TimeSpan.Parse("07:00:00"), summary.Out1);
            Assert.Equal(TimeSpan.Parse("09:30:00"), summary.Out2);
            Assert.Equal(TimeSpan.Parse("12:00:00"), summary.Out3);
            Assert.Equal(TimeSpan.Parse("14:30:00"), summary.Out4);
        }
        [Fact]
        public void Build_TwoBreaksInOneDay_ActualTimeUsesThreeSegmentSum()
        {
            // Arrange: 3 trips, 2 gaps over the break threshold — this hits
            // inOutList.Count == 4, i.e. CalculateActualTime's "case 4" branch
            // (sum1 = Out1-Start, sum2 = Out2-In1, sum3 = End-In2).
            var driver = MakeDriver(2, "Doe, Jane");
            var tripDate = MakeTripDate(501, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(10, 2, tripDate, "06:00:00", "06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00", "20:00:00"),
                MakeTrip(11, 2, tripDate, "06:00:00", "09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00", "20:00:00"),
                MakeTrip(12, 2, tripDate, "06:00:00", "11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00", "20:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            var groups = TripGrouper.GroupTrips(trips);
            var acc = AccumulatorBuilder.Build(groups[0]);
            var weeklyTime = TimeSpan.Zero;

            // Act
            var summary = SummaryBuilder.Build(groups[0], acc, drivers, dates, ref weeklyTime);

            // Assert: exactly 2 breaks (Out1/In1, Out2/In2 populated; Out3/In3/Out4/In4 stay zero)
            Assert.NotEqual(TimeSpan.Zero, summary.Out1);
            Assert.NotEqual(TimeSpan.Zero, summary.In1);
            Assert.NotEqual(TimeSpan.Zero, summary.Out2);
            Assert.NotEqual(TimeSpan.Zero, summary.In2);
            Assert.Equal(TimeSpan.Zero, summary.Out3);
            Assert.Equal(TimeSpan.Zero, summary.Out4);

            // ActualTime should equal the sum of the three worked segments:
            // (Out1-Start) + (Out2-In1) + (End-In2)
            var expected = summary.Out1.Subtract(summary.Start)
                .Add(summary.Out2.Subtract(summary.In1))
                .Add(summary.End.Subtract(summary.In2));

            Assert.Equal(expected, summary.ActualTime);
        }

        [Fact]
        public void Build_ThreeBreaksInOneDay_ActualTimeUsesFourSegmentSum()
        {
            // Arrange: 4 trips, 3 gaps over the break threshold — this hits
            // inOutList.Count == 6, i.e. CalculateActualTime's "case 6" branch
            // (sum1 = Out1-Start, sum2 = Out2-In1, sum3 = Out3-In2, sum4 = End-In3).
            var driver = MakeDriver(3, "Park, Sam");
            var tripDate = MakeTripDate(502, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(20, 3, tripDate, "06:00:00", "06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00", "20:00:00"),
                MakeTrip(21, 3, tripDate, "06:00:00", "09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00", "20:00:00"),
                MakeTrip(22, 3, tripDate, "06:00:00", "11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00", "20:00:00"),
                MakeTrip(23, 3, tripDate, "06:00:00", "14:00:00", "13:55:00", "14:00:00", "14:30:00", "14:30:00", "20:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            var groups = TripGrouper.GroupTrips(trips);
            var acc = AccumulatorBuilder.Build(groups[0]);
            var weeklyTime = TimeSpan.Zero;

            // Act
            var summary = SummaryBuilder.Build(groups[0], acc, drivers, dates, ref weeklyTime);

            // Assert: exactly 3 breaks (Out1-In3 populated; Out4/In4 stay zero)
            Assert.NotEqual(TimeSpan.Zero, summary.Out1);
            Assert.NotEqual(TimeSpan.Zero, summary.Out2);
            Assert.NotEqual(TimeSpan.Zero, summary.Out3);
            Assert.NotEqual(TimeSpan.Zero, summary.In3);
            Assert.Equal(TimeSpan.Zero, summary.Out4);
            Assert.Equal(TimeSpan.Zero, summary.In4);

            // ActualTime should equal the sum of the four worked segments:
            // (Out1-Start) + (Out2-In1) + (Out3-In2) + (End-In3)
            var expected = summary.Out1.Subtract(summary.Start)
                .Add(summary.Out2.Subtract(summary.In1))
                .Add(summary.Out3.Subtract(summary.In2))
                .Add(summary.End.Subtract(summary.In3));

            Assert.Equal(expected, summary.ActualTime);
        }
    }

}
