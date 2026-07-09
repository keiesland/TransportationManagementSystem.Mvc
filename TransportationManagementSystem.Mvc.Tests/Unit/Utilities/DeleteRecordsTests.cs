using Microsoft.EntityFrameworkCore;
using Moq;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Repositories.Interfaces;
using TransportationManagementSystem.Mvc.UnitOfWork;
using TransportationManagementSystem.Mvc.Utilities;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Utilities
{
    public class DeleteRecordsTests
    {
        private static TripContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new TripContext(options);
        }

        private static Driver MakeDriver(int driverId, string lastName, string firstName, string fullName)
        {
            return new Driver { DriverId = driverId, LastName = lastName, FirstName = firstName, FullName = fullName };
        }

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

        private static Summary MakeSummary(int summaryId, Driver driver, TripDate tripDate)
        {
            return new Summary
            {
                SummaryId = summaryId,
                DriverId = driver.DriverId,
                Driver = driver,
                TripDateId = tripDate.TripDateId,
                TripDate = tripDate,
                Start = TimeSpan.FromHours(8),
                Out1 = TimeSpan.FromHours(8.5),
                In1 = TimeSpan.FromHours(8.4),
                Out2 = TimeSpan.FromHours(8.5),
                In2 = TimeSpan.FromHours(9),
                Out3 = TimeSpan.FromHours(9),
                In3 = TimeSpan.FromHours(9),
                Out4 = TimeSpan.FromHours(9),
                In4 = TimeSpan.FromHours(9),
                End = TimeSpan.FromHours(9),
                ActualTime = TimeSpan.FromHours(17),
                WeeklyTime = "31:05"
            };
        }
            
        
        [Fact]
        public async Task DeleteAllTripsAsync_CallsBulkDelete()
        {
            var date1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date2 = MakeTripDate(2, new DateTime(2026, 6, 2), 22);

            using var db = CreateInMemoryContext(
                nameof(DeleteAllTripsAsync_CallsBulkDelete));

            db.Trips.Add(MakeTrip(10, 2, date2));
            db.Trips.Add(MakeTrip(11, 1, date1));

            await db.SaveChangesAsync();


            var mockBulkOps = new Mock<IBulkOperationsProvider>();

            var data = new TripUnitOfWork(
                db,
                mockBulkOps.Object);


            await DeleteRecords.DeleteAllTripsAsync(data);
            
            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Trip>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllSummariesAsync_CallsBulkDelete()
        {
            var date3 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var date4 = MakeTripDate(2, new DateTime(2026, 6, 2), 22);
            var driver = MakeDriver(1, "Smith", "James", "James Smith");

            using var db = CreateInMemoryContext(
                nameof(DeleteAllSummariesAsync_CallsBulkDelete));

            db.Summaries.Add(MakeSummary(1, driver, date3));
            db.Summaries.Add(MakeSummary(2, driver, date4));
        
            await db.SaveChangesAsync();


            var mockBulkOps = new Mock<IBulkOperationsProvider>();

            var data = new TripUnitOfWork(
                db,
                mockBulkOps.Object);


            await DeleteRecords.DeleteAllSummariesAsync(data);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Summary>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllTripDatesAsync_CallsBulkDelete()
        {
            using var db = CreateInMemoryContext(
                nameof(DeleteAllTripDatesAsync_CallsBulkDelete));

            db.TripDates.Add(MakeTripDate(1, new DateTime(2026, 6, 1), 22));
            db.TripDates.Add(MakeTripDate(2, new DateTime(2026, 6, 2), 22));

            await db.SaveChangesAsync();


            var mockBulkOps = new Mock<IBulkOperationsProvider>();

            var data = new TripUnitOfWork(
                db,
                mockBulkOps.Object);


            await DeleteRecords.DeleteAllTripDatesAsync(data);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<TripDate>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllDriversAsync_CallsBulkDelete()
        {
            using var db = CreateInMemoryContext(
                nameof(DeleteAllDriversAsync_CallsBulkDelete));

            db.Drivers.Add(MakeDriver(10, "Wilson", "Jackson", "Jackson Wilson"));
            db.Drivers.Add(MakeDriver(11, "Haggerty", "Harlow", "Harlow Haggerty"));

            await db.SaveChangesAsync();


            var mockBulkOps = new Mock<IBulkOperationsProvider>();

            var data = new TripUnitOfWork(
                db,
                mockBulkOps.Object);


            await DeleteRecords.DeleteAllDriversAsync(data);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Driver>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAllTableDataAsync_CallsBulkDelete()
        {
            using var db = CreateInMemoryContext(
                nameof(DeleteAllTableDataAsync_CallsBulkDelete));

            var tripDate1 = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var tripDate2 = MakeTripDate(2, new DateTime(2026, 6, 2), 22);

            var driver1 = MakeDriver(10, "Wilson", "Jackson", "Jackson Wilson");
            var driver2 = MakeDriver(11, "Haggerty", "Harlow", "Harlow Haggerty");

            var trip1 = MakeTrip(1, 10, tripDate1);
            var trip2 = MakeTrip(2, 11, tripDate2);

            var summary1 = MakeSummary(1, driver1, tripDate1);
            var summary2 = MakeSummary(2, driver2, tripDate2);

            db.TripDates.Add(tripDate1);
            db.TripDates.Add(tripDate2);

            db.Trips.Add(trip1);
            db.Trips.Add(trip2);

            db.Summaries.Add(summary1);
            db.Summaries.Add(summary2);

            db.Drivers.Add(driver1);
            db.Drivers.Add(driver2);
            
            await db.SaveChangesAsync();

            var mockBulkOps = new Mock<IBulkOperationsProvider>();

            var data = new TripUnitOfWork(db, mockBulkOps.Object);


            await DeleteRecords.DeleteAllTableDataAsync(data);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Trip>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Summary>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Driver>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mockBulkOps.Verify(
                x => x.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<TripDate>>(list => list.Count() == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }


    }
}
