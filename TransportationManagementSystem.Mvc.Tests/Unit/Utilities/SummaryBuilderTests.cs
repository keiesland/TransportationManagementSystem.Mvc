using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Utilities;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Utilities
{
    public class SummaryBuilderTests
    {
        private static Driver MakeDriver(int driverId, string fullName) =>
            new Driver { DriverId = driverId, FullName = fullName };

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber) =>
            new TripDate { TripDateId = tripDateId, Date = date, WeekNumber = weekNumber };

        private static DriverDayGroup MakeGroup(
            int driverId,
            int tripDateId,
            Driver driver,
            TripDate tripDate,
            string actualStart,
            string actualEnd,
            params (string sched, string arrival, string pickup, string dropoff, string schedDropoff)[] trips)
        {
            var day = new DriverDay
            {
                Driver = driver.FullName,
                TripDate = tripDate.Date,
                WeekNumber = tripDate.WeekNumber,
                TripActualStart = TimeSpan.Parse(actualStart),
                TripActualEnd = TimeSpan.Parse(actualEnd)
            };

            foreach (var t in trips)
            {
                day.Trips.Add(new TripSegment
                {
                    ScheduledPickupTime = TimeSpan.Parse(t.sched),
                    PickupArrivalTime = TimeSpan.Parse(t.arrival),
                    ActualPickupTime = TimeSpan.Parse(t.pickup),
                    ActualDropoffTime = TimeSpan.Parse(t.dropoff),
                    ScheduledDropoffTime = TimeSpan.Parse(t.schedDropoff),
                    IsNoShow = false
                });
            }

            return new DriverDayGroup
            {
                DriverId = driverId,
                TripDateId = tripDateId,
                DriverDay = day
            };
        }

        [Fact]
        public void Build_FourBreaksInOneDay_PopulatesAllEightOutInFields()
        {
            var driver = MakeDriver(1, "Lee, Pat");
            var tripDate = MakeTripDate(500, new DateTime(2026, 6, 1), 22);

            var group = MakeGroup(1, 500, driver, tripDate, "06:00:00", "20:00:00",
                ("06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00"),
                ("09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00"),
                ("11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00"),
                ("14:00:00", "13:55:00", "14:00:00", "14:30:00", "14:30:00"),
                ("16:30:00", "16:25:00", "16:30:00", "17:00:00", "17:00:00"));

            var dates = new List<TripDate> { tripDate };
            var weeklyTime = TimeSpan.Zero;

            var summary = SummaryBuilder.Build(group, dates, ref weeklyTime);

            Assert.Equal(TimeSpan.Parse("06:00:00"), summary.Start);
            Assert.Equal(TimeSpan.Parse("17:30:00"), summary.End);

            Assert.Equal(TimeSpan.Parse("07:00:00"), summary.Out1);
            Assert.Equal(TimeSpan.Parse("08:30:00"), summary.In1);
            Assert.Equal(TimeSpan.Parse("09:30:00"), summary.Out2);
            Assert.Equal(TimeSpan.Parse("11:00:00"), summary.In2);
            Assert.Equal(TimeSpan.Parse("12:00:00"), summary.Out3);
            Assert.Equal(TimeSpan.Parse("13:30:00"), summary.In3);
            Assert.Equal(TimeSpan.Parse("14:30:00"), summary.Out4);
            Assert.Equal(TimeSpan.Parse("16:00:00"), summary.In4);

            Assert.Equal(TimeSpan.Parse("05:30:00"), summary.ActualTime);
        }

        [Fact]
        public void Build_TwoBreaksInOneDay_ActualTimeUsesThreeSegmentSum()
        {
            var driver = MakeDriver(2, "Doe, Jane");
            var tripDate = MakeTripDate(501, new DateTime(2026, 6, 1), 22);

            var group = MakeGroup(2, 501, driver, tripDate, "06:00:00", "20:00:00",
                ("06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00"),
                ("09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00"),
                ("11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00"));

            var dates = new List<TripDate> { tripDate };
            var weeklyTime = TimeSpan.Zero;

            var summary = SummaryBuilder.Build(group, dates, ref weeklyTime);

            Assert.Equal(TimeSpan.Parse("06:00:00"), summary.Start);
            Assert.Equal(TimeSpan.Parse("12:30:00"), summary.End);

            Assert.Equal(TimeSpan.Parse("07:00:00"), summary.Out1);
            Assert.Equal(TimeSpan.Parse("08:30:00"), summary.In1);
            Assert.Equal(TimeSpan.Parse("09:30:00"), summary.Out2);
            Assert.Equal(TimeSpan.Parse("11:00:00"), summary.In2);
            Assert.Equal(TimeSpan.Zero, summary.Out3);
            Assert.Equal(TimeSpan.Zero, summary.Out4);

            Assert.Equal(TimeSpan.Parse("03:30:00"), summary.ActualTime);
        }

        [Fact]
        public void Build_ThreeBreaksInOneDay_ActualTimeUsesFourSegmentSum()
        {
            var driver = MakeDriver(3, "Park, Sam");
            var tripDate = MakeTripDate(502, new DateTime(2026, 6, 1), 22);

            var group = MakeGroup(3, 502, driver, tripDate, "06:00:00", "20:00:00",
                ("06:30:00", "06:25:00", "06:30:00", "07:00:00", "07:00:00"),
                ("09:00:00", "08:55:00", "09:00:00", "09:30:00", "09:30:00"),
                ("11:30:00", "11:25:00", "11:30:00", "12:00:00", "12:00:00"),
                ("14:00:00", "13:55:00", "14:00:00", "14:30:00", "14:30:00"));

            var dates = new List<TripDate> { tripDate };
            var weeklyTime = TimeSpan.Zero;

            var summary = SummaryBuilder.Build(group, dates, ref weeklyTime);

            Assert.Equal(TimeSpan.Parse("06:00:00"), summary.Start);
            Assert.Equal(TimeSpan.Parse("15:00:00"), summary.End);

            Assert.Equal(TimeSpan.Parse("07:00:00"), summary.Out1);
            Assert.Equal(TimeSpan.Parse("08:30:00"), summary.In1);
            Assert.Equal(TimeSpan.Parse("09:30:00"), summary.Out2);
            Assert.Equal(TimeSpan.Parse("11:00:00"), summary.In2);
            Assert.Equal(TimeSpan.Parse("12:00:00"), summary.Out3);
            Assert.Equal(TimeSpan.Parse("13:30:00"), summary.In3);
            Assert.Equal(TimeSpan.Zero, summary.Out4);
            Assert.Equal(TimeSpan.Zero, summary.In4);

            Assert.Equal(TimeSpan.Parse("04:30:00"), summary.ActualTime);
        }
    }
}