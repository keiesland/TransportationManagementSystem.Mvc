using Microsoft.EntityFrameworkCore;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.Query;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Repositories;

namespace TransportationManagementSystem.Mvc.Tests.Data
{
    public class RepositoryTests
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
        public void Count_ReturnsNumberOfRows()
        {
            using var db = CreateInMemoryContext(nameof(Count_ReturnsNumberOfRows));

            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            db.Drivers.Add(MakeDriver(2, "Wilson", "Pat", "Pat Wilson"));
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            Assert.Equal(2, repository.Count());
        }

        [Fact]
        public void Insert_AddsEntityToContext()
        {
            using var db = CreateInMemoryContext(nameof(Insert_AddsEntityToContext));

            var repository = new Repository<Driver>(db);

            repository.Insert(
                MakeDriver(1, "Adams", "Sam", "Sam Adams"));

            Assert.Single(db.Drivers.Local);
        }

        [Fact]
        public void Update_MarksEntityModified()
        {
            using var db = CreateInMemoryContext(nameof(Update_MarksEntityModified));

            var driver = MakeDriver(1, "Adams", "Sam", "Sam Adams");

            db.Drivers.Add(driver);
            db.SaveChanges();

            driver.LastName = "Smith";

            var repository = new Repository<Driver>(db);

            repository.Update(driver);

            Assert.Equal(
                EntityState.Modified,
                db.Entry(driver).State);
        }

        [Fact]
        public void Delete_MarksEntityDeleted()
        {
            using var db = CreateInMemoryContext(nameof(Delete_MarksEntityDeleted));

            var driver = MakeDriver(1, "Adams", "Sam", "Sam Adams");

            db.Drivers.Add(driver);
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            repository.Delete(driver);

            Assert.Equal(
                EntityState.Deleted,
                db.Entry(driver).State);
        }

        [Fact]
        public async Task AnyAsync_WhenRowsExist_ReturnsTrue()
        {
            using var db = CreateInMemoryContext(nameof(AnyAsync_WhenRowsExist_ReturnsTrue));

            db.Drivers.Add(
                MakeDriver(1, "Adams", "Sam", "Sam Adams"));

            await db.SaveChangesAsync();

            var repository = new Repository<Driver>(db);

            Assert.True(await repository.AnyAsync());
        }

        [Fact]
        public async Task AnyAsync_WhenNoRowsExist_ReturnsFalse()
        {
            using var db = CreateInMemoryContext(nameof(AnyAsync_WhenNoRowsExist_ReturnsFalse));

            var repository = new Repository<Driver>(db);

            Assert.False(await repository.AnyAsync());
        }

        [Fact]
        public void Get_ReturnsMatchingRow()
        {
            using var db = CreateInMemoryContext(nameof(Get_ReturnsMatchingRow));

            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            db.Drivers.Add(MakeDriver(2, "Wilson", "Pat", "Pat Wilson"));
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>
            {
                Where = d => d.DriverId == 2
            };

            var result = repository.Get(options);

            Assert.NotNull(result);
            Assert.Equal(2, result.DriverId);
        }

        [Fact]
        public async Task GetAsync_ReturnsMatchingRow()
        {
            using var db = CreateInMemoryContext(nameof(GetAsync_ReturnsMatchingRow));

            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            await db.SaveChangesAsync();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>();
            options.Where = d => d.DriverId == 1;

            var result = await repository.GetAsync(options);

            Assert.NotNull(result);
            Assert.Equal(1, result.DriverId);
        }

        [Fact]
        public void List_ReturnsAllRows()
        {
            using var db = CreateInMemoryContext(nameof(List_ReturnsAllRows));

            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            db.Drivers.Add(MakeDriver(2, "Wilson", "Pat", "Pat Wilson"));
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>();

            var result = repository.List(options);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task ListAsync_ReturnsAllRows()
        {
            using var db = CreateInMemoryContext(nameof(ListAsync_ReturnsAllRows));

            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            await db.SaveChangesAsync();

            var repository = new Repository<Driver>(db);

            var result = await repository.ListAsync(
                new QueryOptions<Driver>());

            Assert.Single(result);
        }

        [Fact]
        public void List_AppliesAscendingOrder()
        {
            using var db = CreateInMemoryContext(nameof(List_AppliesAscendingOrder));

            db.Drivers.Add(MakeDriver(2, "Wilson", "Pat", "Pat Wilson"));
            db.Drivers.Add(MakeDriver(1, "Adams", "Sam", "Sam Adams"));
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>
            {
                OrderBy = d => d.LastName
            };

            var result = repository.List(options);

            Assert.Equal("Adams", result[0].LastName);
            Assert.Equal("Wilson", result[1].LastName);
        }

        [Fact]
        public void List_AppliesPaging()
        {
            using var db = CreateInMemoryContext(nameof(List_AppliesPaging));

            for (int i = 1; i <= 10; i++)
            {
                db.Drivers.Add(
                    MakeDriver(i, $"Last{i}", $"First{i}", $"Driver{i}"));
            }

            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>
            {
                PageNumber = 2,
                PageSize = 3
            };

            var result = repository.List(options);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void List_WithInclude_LoadsRelatedData()
        {
            using var db = CreateInMemoryContext(nameof(List_WithInclude_LoadsRelatedData));

            var driver = new Driver
            {
                DriverId = 1,
                LastName = "Adams",
                FirstName = "Sam",
                FullName = "Sam Adams",
                Trips = new List<Trip>
                {
                    new Trip
                    {
                        TripId = 1
                    }
                }
            };

            db.Drivers.Add(driver);
            db.SaveChanges();

            var repository = new Repository<Driver>(db);

            var options = new QueryOptions<Driver>
            {
                Includes = "Trips"
            };

            var result = repository.Get(options);

            Assert.NotNull(result);
            Assert.NotNull(result.Trips);
            Assert.Single(result.Trips);
        }
    }
}
