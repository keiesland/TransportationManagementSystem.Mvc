using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Services
{
    public class AggregationServiceTests
    {
        private readonly AggregationService _service = new();

        private static TripImportRow MakeRow(
            string driver,
            DateTime tripDate,
            int weekNumber = 39,
            string tripActualStart = "06:00:00",
            string tripActualEnd = "18:00:00",
            string scheduledPickup = "06:30:00",
            string pickupArrival = "06:25:00",
            string actualPickup = "06:30:00",
            string actualDropoff = "07:00:00",
            string scheduledDropoff = "07:00:00")
        {
            return new TripImportRow
            {
                Driver = driver,
                TripDate = tripDate,
                WeekNumber = weekNumber,
                TripActualStartTime = TimeSpan.Parse(tripActualStart),
                TripActualEndTime = TimeSpan.Parse(tripActualEnd),
                ScheduledPickupTime = TimeSpan.Parse(scheduledPickup),
                PickupArrivalTime = TimeSpan.Parse(pickupArrival),
                ActualPickupTime = TimeSpan.Parse(actualPickup),
                ActualDropoffTime = TimeSpan.Parse(actualDropoff),
                ScheduledDropoffTime = TimeSpan.Parse(scheduledDropoff)
            };
        }

        // ===== Grouping correctness =====

        [Fact]
        public void Aggregate_SameDriverAndDate_GroupsIntoOneDriverDay()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23)),
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23))
            };

            var result = _service.Aggregate(rows);

            Assert.Single(result);
            Assert.Equal(2, result[0].Trips.Count);
        }

        [Fact]
        public void Aggregate_SameDriverDifferentDates_CreatesSeparateDriverDays()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23)),
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 24))
            };

            var result = _service.Aggregate(rows);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Aggregate_DifferentDriversSameDate_CreatesSeparateDriverDays()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23)),
                MakeRow("King, Robert", new DateTime(2024, 9, 23))
            };

            var result = _service.Aggregate(rows);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.Driver == "Bango, Stephen");
            Assert.Contains(result, d => d.Driver == "King, Robert");
        }

        // ===== Sorting =====

        [Fact]
        public void Aggregate_ResultsSortedByDriverThenDate()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Wilson, Tomika", new DateTime(2024, 9, 24)),
                MakeRow("Wilson, Tomika", new DateTime(2024, 9, 23)),
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23))
            };

            var result = _service.Aggregate(rows);

            Assert.Equal("Bango, Stephen", result[0].Driver);
            Assert.Equal("Wilson, Tomika", result[1].Driver);
            Assert.Equal(new DateTime(2024, 9, 23), result[1].TripDate);
            Assert.Equal("Wilson, Tomika", result[2].Driver);
            Assert.Equal(new DateTime(2024, 9, 24), result[2].TripDate);
        }

        // ===== DriverDay field mapping =====

        [Fact]
        public void Aggregate_PopulatesDriverDayFields_FromFirstRowInGroup()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Bango, Stephen", new DateTime(2024, 9, 23),
                    weekNumber: 39, tripActualStart: "12:30:00", tripActualEnd: "15:46:00")
            };

            var result = _service.Aggregate(rows);

            var day = result[0];
            Assert.Equal("Bango, Stephen", day.Driver);
            Assert.Equal(new DateTime(2024, 9, 23), day.TripDate);
            Assert.Equal(39, day.WeekNumber);
            Assert.Equal(TimeSpan.Parse("12:30:00"), day.TripActualStart);
            Assert.Equal(TimeSpan.Parse("15:46:00"), day.TripActualEnd);
        }

        [Fact]
        public void Aggregate_MapsTripSegmentFieldsCorrectly()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Wade, Donald", new DateTime(2024, 9, 24),
                    scheduledPickup: "12:30:00", pickupArrival: "12:34:00",
                    actualPickup: "12:41:00", actualDropoff: "13:07:00",
                    scheduledDropoff: "12:55:00")
            };

            var result = _service.Aggregate(rows);

            var segment = result[0].Trips[0];
            Assert.Equal(TimeSpan.Parse("12:30:00"), segment.ScheduledPickupTime);
            Assert.Equal(TimeSpan.Parse("12:34:00"), segment.PickupArrivalTime);
            Assert.Equal(TimeSpan.Parse("12:41:00"), segment.ActualPickupTime);
            Assert.Equal(TimeSpan.Parse("13:07:00"), segment.ActualDropoffTime);
            Assert.Equal(TimeSpan.Parse("12:55:00"), segment.ScheduledDropoffTime);
        }

        // ===== No-show detection =====

        [Fact]
        public void Aggregate_ZeroActualPickupAndDropoff_MarksTripAsNoShow()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Wade, Donald", new DateTime(2024, 10, 4),
                    actualPickup: "00:00:00", actualDropoff: "00:00:00")
            };

            var result = _service.Aggregate(rows);

            Assert.True(result[0].Trips[0].IsNoShow);
        }

        [Fact]
        public void Aggregate_ZeroPickupButNonZeroDropoff_IsNotMarkedNoShow()
        {
            // Only BOTH being zero counts as a no-show -- one alone shouldn't.
            var rows = new List<TripImportRow>
            {
                MakeRow("Wade, Donald", new DateTime(2024, 10, 4),
                    actualPickup: "00:00:00", actualDropoff: "13:07:00")
            };

            var result = _service.Aggregate(rows);

            Assert.False(result[0].Trips[0].IsNoShow);
        }

        [Fact]
        public void Aggregate_NonZeroPickupAndDropoff_IsNotMarkedNoShow()
        {
            var rows = new List<TripImportRow>
            {
                MakeRow("Wade, Donald", new DateTime(2024, 10, 4),
                    actualPickup: "12:41:00", actualDropoff: "13:07:00")
            };

            var result = _service.Aggregate(rows);

            Assert.False(result[0].Trips[0].IsNoShow);
        }

        // ===== Multi-load / overlapping trips (preserved order) =====

        [Fact]
        public void Aggregate_MultipleOverlappingTripsForSameDriverDay_PreservesAllTripSegments()
        {
            // Doesn't assert ordering here -- DriverDay.Breaks/Start/End are
            // already independently verified to handle unordered/overlapping
            // segments correctly. This just confirms Aggregate doesn't drop
            // or merge any of them.
            var rows = new List<TripImportRow>
            {
                MakeRow("Massas, Hermann", new DateTime(2024, 9, 23),
                    scheduledPickup: "13:30:00", actualDropoff: "14:05:00"),
                MakeRow("Massas, Hermann", new DateTime(2024, 9, 23),
                    scheduledPickup: "13:30:00", actualDropoff: "14:23:00"),
                MakeRow("Massas, Hermann", new DateTime(2024, 9, 23),
                    scheduledPickup: "13:30:00", actualDropoff: "13:58:00")
            };

            var result = _service.Aggregate(rows);

            Assert.Single(result);
            Assert.Equal(3, result[0].Trips.Count);
        }

        // ===== Edge case: empty input =====

        [Fact]
        public void Aggregate_EmptyRowList_ReturnsEmptyList()
        {
            var result = _service.Aggregate(new List<TripImportRow>());

            Assert.Empty(result);
        }
    }
}