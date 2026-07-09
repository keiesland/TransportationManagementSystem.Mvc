using Microsoft.EntityFrameworkCore;
using Moq;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Repositories;
using TransportationManagementSystem.Mvc.Repositories.Interfaces;

namespace TransportationManagementSystem.Mvc.Tests.Integration.Data
{
    public class RepositoryBulkOperationTests
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
            return new Driver
            {
                DriverId = driverId,
                LastName = lastName,
                FirstName = firstName,
                FullName = fullName
            };
        }

        [Fact]
        public async Task BulkInsertAsync_DelegatesToProvider_WithSameEntitiesAndContext()
        {
            // Arrange
            using var db = CreateInMemoryContext(nameof(BulkInsertAsync_DelegatesToProvider_WithSameEntitiesAndContext));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var repository = new Repository<Driver>(db, mockBulkOps.Object);

            var drivers = new List<Driver>
            {
                MakeDriver(1, "Adams", "Samuel", "Samuel Adams"),
                MakeDriver(2, "Wilson", "Patrick", "Patrick Wilson")
            };

            // Act
            await repository.BulkInsertAsync(drivers);

            // Assert: verify the provider was called once, with our exact list
            mockBulkOps.Verify(
                p => p.BulkInsertAsync(db, drivers, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAsync_DelegatesToProvider_WithSameEntitiesAndContext()
        {
            using var db = CreateInMemoryContext(nameof(BulkDeleteAsync_DelegatesToProvider_WithSameEntitiesAndContext));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var repository = new Repository<Driver>(db, mockBulkOps.Object);

            var drivers = new List<Driver> { MakeDriver(1, "Adams", "Samuel", "Samuel Adams") };

            await repository.BulkDeleteAsync(drivers);

            mockBulkOps.Verify(
                p => p.BulkDeleteAsync(db, drivers, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAllAsync_TableHasRows_CallsProviderWithAllRows()
        {
            // Arrange: seed the InMemory DbSet directly (no bulk ops involved here —
            // this is just normal EF Core, which InMemory handles fine)
            using var db = CreateInMemoryContext(nameof(BulkDeleteAllAsync_TableHasRows_CallsProviderWithAllRows));
            db.Drivers.Add(MakeDriver(1, "Adams", "Samuel", "Samuel Adams"));
            db.Drivers.Add(MakeDriver(2, "Wilson", "Patrick", "Patrick Wilson"));
            await db.SaveChangesAsync();

            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var repository = new Repository<Driver>(db, mockBulkOps.Object);

            // Act
            await repository.BulkDeleteAllAsync();

            // Assert: provider was called once with exactly the 2 seeded rows
            mockBulkOps.Verify(
                p => p.BulkDeleteAsync(
                    db,
                    It.Is<IEnumerable<Driver>>(list => System.Linq.Enumerable.Count(list) == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAllAsync_OnEmptyTable_NeverCallsProvider()
        {
            // Regression test: the AnyAsync() short-circuit inside
            // BulkDeleteAllAsync should prevent calling the bulk provider
            // at all when the table is already empty.
            using var db = CreateInMemoryContext(nameof(BulkDeleteAllAsync_OnEmptyTable_NeverCallsProvider));
            var mockBulkOps = new Mock<IBulkOperationsProvider>();
            var repository = new Repository<Driver>(db, mockBulkOps.Object);

            await repository.BulkDeleteAllAsync();

            mockBulkOps.Verify(
                p => p.BulkDeleteAsync(It.IsAny<DbContext>(), It.IsAny<IEnumerable<Driver>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

    }
}
