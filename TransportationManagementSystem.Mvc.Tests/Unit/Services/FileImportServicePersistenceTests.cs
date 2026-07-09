using Microsoft.EntityFrameworkCore;
using Moq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Services;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.TestHelpers;
using TransportationManagementSystem.Mvc.Tests.TestHelpers;
using TransportationManagementSystem.Mvc.UnitOfWork;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Services
{
    public class FileImportServicePersistenceTests
    {
        private readonly Mock<IAggregationService> _mockAggregationService;
        private readonly Mock<IValidationService> _mockValidationService;

        public FileImportServicePersistenceTests()
        {
            _mockAggregationService = new Mock<IAggregationService>();
            _mockValidationService = new Mock<IValidationService>();

            _mockAggregationService
                .Setup(s => s.Aggregate(It.IsAny<List<TripImportRow>>()))
                .Returns(new List<DriverDay>());

            // Default: validation passes, so PersistRowsAsync actually runs.
            _mockValidationService
                .Setup(s => s.Validate(It.IsAny<List<DriverDay>>()))
                .Returns(new ValidationResult { IsValid = true, Errors = new List<ValidationError>() });
        }

        private static TripUnitOfWork MakeUnitOfWork(out TripContext context)
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            context = new TripContext(options);
            return new TripUnitOfWork(context, new FakeBulkOperationsProvider());
        }

        private static MemoryStream BuildWorkbook(params (string Driver, DateTime Date, string Dropoff)[] rows)
        {
            var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            var header = sheet.CreateRow(0);
            for (int c = 0; c < 9; c++) header.CreateCell(c).SetCellValue($"Col{c}");

            int rowIndex = 1;
            foreach (var r in rows)
            {
                var row = sheet.CreateRow(rowIndex++);
                row.CreateCell(0).SetCellValue(r.Driver);
                row.CreateCell(1).SetCellValue(r.Date);
                row.CreateCell(6).SetCellValue(r.Date.Add(TimeSpan.Parse(r.Dropoff)));
            }

            var stream = new MemoryStream();
            workbook.Write(new NonClosingStreamWrapper(stream));
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public async Task ImportTripsAsync_ValidFile_CreatesNewDriverAndTripDate()
        {
            var uow = MakeUnitOfWork(out var context);
            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(context.Drivers);
            Assert.Equal("Bango, Stephen", context.Drivers.First().FullName);
            Assert.Single(context.TripDates);
            Assert.Equal(new DateTime(2024, 9, 23), context.TripDates.First().Date);
        }

        [Fact]
        public async Task ImportTripsAsync_NewDriver_SplitsFullNameIntoLastAndFirst()
        {
            var uow = MakeUnitOfWork(out var context);
            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            var driver = context.Drivers.First();
            Assert.Equal("Bango", driver.LastName);
            Assert.Equal("Stephen", driver.FirstName);
        }

        [Fact]
        public async Task ImportTripsAsync_SameDriverMultipleRows_CreatesOnlyOneDriverRecord()
        {
            var uow = MakeUnitOfWork(out var context);
            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(
                ("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"),
                ("Bango, Stephen", new DateTime(2024, 9, 24), "14:00:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(context.Drivers);
            Assert.Equal(2, context.TripDates.Count());
        }

        [Fact]
        public async Task ImportTripsAsync_DriverAlreadyExists_ReusesExistingDriverRecord()
        {
            var uow = MakeUnitOfWork(out var context);
            context.Drivers.Add(new Driver { FullName = "Bango, Stephen", LastName = "Bango", FirstName = "Stephen" });
            await context.SaveChangesAsync();

            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(context.Drivers); // still just the one pre-existing record
        }

        [Fact]
        public async Task ImportTripsAsync_TripDateAlreadyExists_ReusesExistingTripDateRecord()
        {
            var uow = MakeUnitOfWork(out var context);
            var existingDate = new DateTime(2024, 9, 23);
            context.TripDates.Add(new TripDate { Date = existingDate, WeekNumber = 39 });
            await context.SaveChangesAsync();

            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", existingDate, "13:13:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(context.TripDates);
        }

        [Fact]
        public async Task ImportTripsAsync_ValidFile_InsertsCorrectNumberOfTrips()
        {
            var uow = MakeUnitOfWork(out var context);
            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(
                ("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"),
                ("King, Robert", new DateTime(2024, 9, 24), "15:07:00"));

            var result = await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Equal(2, result.ImportedCount);
            Assert.Equal(2, context.Trips.Count());
        }

        [Fact]
        public async Task ImportTripsAsync_TripLinkedToCorrectDriverAndTripDateIds()
        {
            var uow = MakeUnitOfWork(out var context);
            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            var trip = context.Trips.First();
            var driver = context.Drivers.First();
            var tripDate = context.TripDates.First();

            Assert.Equal(driver.DriverId, trip.DriverId);
            Assert.Equal(tripDate.TripDateId, trip.TripDateId);
        }

        [Fact]
        public async Task ImportTripsAsync_ExistingTrips_AreDeletedBeforeReimport()
        {
            var uow = MakeUnitOfWork(out var context);
            var driver = new Driver { FullName = "Old, Driver", LastName = "Old", FirstName = "Driver" };
            var tripDate = new TripDate { Date = new DateTime(2024, 1, 1), WeekNumber = 1 };
            context.Drivers.Add(driver);
            context.TripDates.Add(tripDate);
            await context.SaveChangesAsync();

            context.Trips.Add(new Trip { DriverId = driver.DriverId, TripDateId = tripDate.TripDateId });
            await context.SaveChangesAsync();
            Assert.Single(context.Trips); // sanity check

            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));
            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            // Old trip gone, only the newly imported one remains.
            Assert.Single(context.Trips);
            Assert.NotEqual(driver.DriverId, context.Trips.First().DriverId);
        }

        [Fact]
        public async Task ImportTripsAsync_ValidationFails_DoesNotDeleteExistingTripsOrCreateNewRecords()
        {
            var uow = MakeUnitOfWork(out var context);
            var driver = new Driver { FullName = "Existing, Driver", LastName = "Existing", FirstName = "Driver" };
            var tripDate = new TripDate { Date = new DateTime(2024, 1, 1), WeekNumber = 1 };
            context.Drivers.Add(driver);
            context.TripDates.Add(tripDate);
            await context.SaveChangesAsync();
            context.Trips.Add(new Trip { DriverId = driver.DriverId, TripDateId = tripDate.TripDateId });
            await context.SaveChangesAsync();

            _mockValidationService
                .Setup(s => s.Validate(It.IsAny<List<DriverDay>>()))
                .Returns(new ValidationResult
                {
                    IsValid = false,
                    Errors = new List<ValidationError> { new ValidationError { Driver = "X", Message = "bad" } }
                });

            var service = new FileImportService(uow, _mockAggregationService.Object, _mockValidationService.Object);

            using var stream = BuildWorkbook(("Bango, Stephen", new DateTime(2024, 9, 23), "13:13:00"));
            var result = await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.Single(context.Trips);   // old trip untouched
            Assert.Single(context.Drivers); // no new driver created
            Assert.Single(context.TripDates);
        }
    }
}
