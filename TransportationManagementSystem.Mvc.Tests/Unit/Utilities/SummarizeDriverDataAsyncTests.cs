using TransportationManagementSystem.Data;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TransportationManagementSystem.UnitOfWork;

namespace TransportationManagementSystem.Tests.Unit.Utilities
{
    public class SummarizeDriverDataAsyncTests
    {
        // ---------- Shared builders ----------

        private static TripContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new TripContext(options);
        }

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

        // ---------- Tests ----------

        [Fact]
        public async Task SummarizeDriverDataAsync_NoTrips_NeverCallsBulkInsert()
        {
            // Arrange: empty trip list => CalculateSummaries returns an empty
            // list => the "if (summaries.Count > 0)" guard should prevent any
            // call to the bulk provider at all.
            using var db = CreateInMemoryContext(nameof(SummarizeDriverDataAsync_NoTrips_NeverCallsBulkInsert));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var unitOfWork = new TripUnitOfWork(db, mockBulkOps.Object);

            var trips = new List<Trip>();
            var drivers = new List<Driver>();
            var dates = new List<TripDate>();

            // Act
            await SummarizeData.SummarizeDriverDataAsync(unitOfWork, trips, drivers, dates, CancellationToken.None);

            // Assert
            mockBulkOps.Verify(
                p => p.BulkInsertAsync(It.IsAny<DbContext>(), It.IsAny<IEnumerable<Summary>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SummarizeDriverDataAsync_OneDriverOneDay_CallsBulkInsertOnceWithOneSummary()
        {
            // Arrange: a normal single-driver, single-day trip set => exactly
            // one Summary should be calculated and passed to BulkInsertAsync
            // in a single call.
            using var db = CreateInMemoryContext(nameof(SummarizeDriverDataAsync_OneDriverOneDay_CallsBulkInsertOnceWithOneSummary));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var unitOfWork = new TripUnitOfWork(db, mockBulkOps.Object);

            var driver = MakeDriver(1, "Smith, John");
            var tripDate = MakeTripDate(100, new DateTime(2026, 6, 1), 22);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, tripDate, "08:00:00", "08:30:00", "08:25:00", "08:30:00", "09:00:00", "09:00:00", "17:00:00"),
                MakeTrip(2, 1, tripDate, "08:00:00", "09:15:00", "09:10:00", "09:15:00", "10:00:00", "10:00:00", "17:00:00")
            };

            var drivers = new List<Driver> { driver };
            var dates = new List<TripDate> { tripDate };

            // Act
            await SummarizeData.SummarizeDriverDataAsync(unitOfWork, trips, drivers, dates, CancellationToken.None);

            // Assert: BulkInsertAsync called exactly once, with exactly one
            // Summary, for the correct driver and date.
            mockBulkOps.Verify(
                p => p.BulkInsertAsync(
                    db,
                    It.Is<IEnumerable<Summary>>(list =>
                        System.Linq.Enumerable.Count(list) == 1 &&
                        System.Linq.Enumerable.First(list).DriverId == 1 &&
                        System.Linq.Enumerable.First(list).TripDateId == 100),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SummarizeDriverDataAsync_TwoDriverDayGroups_CallsBulkInsertOnceWithBothSummaries()
        {
            // Arrange: two separate driver/day groups => CalculateSummaries
            // should produce 2 Summary records, both passed in the SAME
            // single BulkInsertAsync call (not two separate calls).
            using var db = CreateInMemoryContext(nameof(SummarizeDriverDataAsync_TwoDriverDayGroups_CallsBulkInsertOnceWithBothSummaries));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var unitOfWork = new TripUnitOfWork(db, mockBulkOps.Object);

            var driver1 = MakeDriver(1, "Smith, John");
            var driver2 = MakeDriver(2, "Doe, Jane");
            var date1 = MakeTripDate(100, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(101, new DateTime(2026, 6, 2), 22);

            var trips = new List<Trip>
            {
                MakeTrip(1, 1, date1, "08:00:00", "08:30:00", "08:25:00", "08:30:00", "09:00:00", "09:00:00", "17:00:00"),
                MakeTrip(2, 2, date2, "08:00:00", "08:30:00", "08:25:00", "08:30:00", "09:00:00", "09:00:00", "17:00:00")
            };

            var drivers = new List<Driver> { driver1, driver2 };
            var dates = new List<TripDate> { date1, date2 };

            // Act
            await SummarizeData.SummarizeDriverDataAsync(unitOfWork, trips, drivers, dates, CancellationToken.None);

            // Assert
            mockBulkOps.Verify(
                p => p.BulkInsertAsync(
                    db,
                    It.Is<IEnumerable<Summary>>(list => System.Linq.Enumerable.Count(list) == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

}
