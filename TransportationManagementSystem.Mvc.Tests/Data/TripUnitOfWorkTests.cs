using Microsoft.EntityFrameworkCore;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.UnitOfWork;

namespace TransportationManagementSystem.Mvc.Tests.Data
{
    public class TripUnitOfWorkTests
    {
        private TripContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TripContext(options);
        }

        [Fact]
        public void GetRepository_ReturnsSameInstance()
        {
            using var context = CreateContext();

            var uow = new TripUnitOfWork(context);

            var repo1 = uow.Trips;
            var repo2 = uow.Trips;

            Assert.Same(repo1, repo2);
        }

        [Fact]
        public void DifferentEntities_ReturnDifferentRepositories()
        {
            using var context = CreateContext();

            var uow = new TripUnitOfWork(context);

            Assert.NotSame(uow.Trips, uow.Drivers);
            Assert.NotSame(uow.TripDates, uow.Summaries);
        }

        [Fact]
        public void Save_PersistsChanges()
        {
            // Arrange
            using var context = CreateContext();
            var uow = new TripUnitOfWork(context);

            context.Drivers.Add(new Driver
            {
                DriverId = 1,
                FirstName = "James",
                LastName = "King",
                FullName = "James King"
            });

            // Act
            uow.Save();

            // Assert
            Assert.Equal(1, context.Drivers.Count());
        }

        [Fact]
        public async Task SaveAsync_PersistsChanges()
        {
            // Arrange
            using var context = CreateContext();
            var uow = new TripUnitOfWork(context);

            context.Drivers.Add(new Driver
            {
                    DriverId = 1,
                    FirstName = "James",
                    LastName = "King",
                    FullName = "James King"
            });

            // Act
            await uow.SaveAsync();

            // Assert
            Assert.Equal(1, context.Drivers.Count());
        }
    }
}
