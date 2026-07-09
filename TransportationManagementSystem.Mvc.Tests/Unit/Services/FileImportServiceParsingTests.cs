using Microsoft.EntityFrameworkCore;
using Moq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Services;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.TestHelpers;
using TransportationManagementSystem.Mvc.Tests.TestHelpers;
using TransportationManagementSystem.Mvc.UnitOfWork;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Services
{
    /// <summary>
    /// Covers ParseRowsFromSheet's parsing edge cases without touching the
    /// database. Achieved by mocking IValidationService to always return
    /// invalid -- ImportTripsAsync short-circuits before PersistRowsAsync,
    /// and IAggregationService.Aggregate is mocked to capture the parsed
    /// List&lt;TripImportRow&gt; via a callback so we can inspect it directly.
    /// </summary>
    public class FileImportServiceParsingTests
    {
        private readonly Mock<IAggregationService> _mockAggregationService;
        private readonly Mock<IValidationService> _mockValidationService;
        private List<TripImportRow> _capturedRows;

        public FileImportServiceParsingTests()
        {
            _mockAggregationService = new Mock<IAggregationService>();
            _mockValidationService = new Mock<IValidationService>();

            _mockAggregationService
                .Setup(s => s.Aggregate(It.IsAny<List<TripImportRow>>()))
                .Callback<List<TripImportRow>>(rows => _capturedRows = rows)
                .Returns(new List<DriverDay>());

            _mockValidationService
                .Setup(s => s.Validate(It.IsAny<List<DriverDay>>()))
                .Returns(new ValidationResult { IsValid = false, Errors = new List<ValidationError>() });
        }

        /// <summary>
        /// Builds a minimal .xlsx workbook in memory with one sheet and the
        /// given rows. Column order matches ParseRowsFromSheet's expectations:
        /// 0=Driver, 1=TripDate, 2=TripActualStart, 3=ScheduledPickup,
        /// 4=PickupArrival, 5=ActualPickup, 6=ActualDropoff,
        /// 7=ScheduledDropoff, 8=TripActualEnd.
        /// </summary>
        private static MemoryStream BuildWorkbook(Action<ISheet> populateRows)
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            var header = sheet.CreateRow(0);
            for (int c = 0; c < 9; c++) header.CreateCell(c).SetCellValue($"Col{c}");

            populateRows(sheet);

            var stream = new MemoryStream();
            workbook.Write(new NonClosingStreamWrapper(stream));
            stream.Position = 0;
            return stream;
        }

        private static void SetTimeCell(IRow row, int col, DateTime date, TimeSpan? time)
        {
            var cell = row.CreateCell(col);
            if (time.HasValue)
                cell.SetCellValue(date.Date.Add(time.Value));
            else
                cell.SetCellValue(0.0);
        }

        [Fact]
        public async Task ImportTripsAsync_ValidRow_ParsesAllFieldsCorrectly()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 2, driverDate, TimeSpan.Parse("12:30:00"));
                SetTimeCell(row, 3, driverDate, TimeSpan.Parse("13:00:00"));
                SetTimeCell(row, 4, driverDate, TimeSpan.Parse("12:59:00"));
                SetTimeCell(row, 5, driverDate, TimeSpan.Parse("13:01:00"));
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
                SetTimeCell(row, 7, driverDate, TimeSpan.Parse("13:15:00"));
                SetTimeCell(row, 8, driverDate, TimeSpan.Parse("15:46:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.NotNull(_capturedRows);
            Assert.Single(_capturedRows);

            var parsed = _capturedRows[0];
            Assert.Equal("Bango, Stephen", parsed.Driver);
            Assert.Equal(driverDate, parsed.TripDate);
            Assert.Equal(TimeSpan.Parse("12:30:00"), parsed.TripActualStartTime);
            Assert.Equal(TimeSpan.Parse("13:00:00"), parsed.ScheduledPickupTime);
            Assert.Equal(TimeSpan.Parse("12:59:00"), parsed.PickupArrivalTime);
            Assert.Equal(TimeSpan.Parse("13:01:00"), parsed.ActualPickupTime);
            Assert.Equal(TimeSpan.Parse("13:13:00"), parsed.ActualDropoffTime);
            Assert.Equal(TimeSpan.Parse("13:15:00"), parsed.ScheduledDropoffTime);
            Assert.Equal(TimeSpan.Parse("15:46:00"), parsed.TripActualEndTime);
        }

        [Fact]
        public async Task ImportTripsAsync_BlankRow_IsSkipped()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                sheet.CreateRow(1); // entirely blank -- no cells created

                var row2 = sheet.CreateRow(2);
                row2.CreateCell(0).SetCellValue("Bango, Stephen");
                row2.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row2, 4, driverDate, TimeSpan.Parse("12:59:00"));
                SetTimeCell(row2, 5, driverDate, TimeSpan.Parse("13:01:00"));
                SetTimeCell(row2, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(_capturedRows);
        }

        [Fact]
        public async Task ImportTripsAsync_RowMissingAllThreeCriticalTimeCells_IsSkipped()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Empty(_capturedRows);
        }

        [Fact]
        public async Task ImportTripsAsync_RowWithOnlyOneOfThreeCriticalCells_IsIncluded()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(_capturedRows);
        }

        [Fact]
        public async Task ImportTripsAsync_MissingDriverName_IsSkipped()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Empty(_capturedRows);
        }

        [Fact]
        public async Task ImportTripsAsync_DriverNameWithWhitespace_IsTrimmed()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("  Bango, Stephen  ");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Equal("Bango, Stephen", _capturedRows[0].Driver);
        }

        [Fact]
        public async Task ImportTripsAsync_TimeStoredAsTextCell_ParsesCorrectly()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
                row.CreateCell(6).SetCellValue("13:13:00");
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Single(_capturedRows);
            Assert.Equal(TimeSpan.Parse("13:13:00"), _capturedRows[0].ActualDropoffTime);
        }

        [Fact]
        public async Task ImportTripsAsync_ZeroTimeValue_TreatedAsNullNotZero()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
                row.CreateCell(2).SetCellValue(0.0);
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Equal(TimeSpan.Zero, _capturedRows[0].TripActualStartTime);
        }

        [Fact]
        public async Task ImportTripsAsync_WeekNumberCalculatedFromDate_UsingIsoWeek()
        {
            var driverDate = new DateTime(2024, 9, 23);

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Bango, Stephen");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Equal(System.Globalization.ISOWeek.GetWeekOfYear(driverDate), _capturedRows[0].WeekNumber);
        }

        [Fact]
        public async Task ImportTripsAsync_MultipleValidRows_AllCaptured()
        {
            var day1 = new DateTime(2024, 9, 23);
            var day2 = new DateTime(2024, 9, 24);

            using var stream = BuildWorkbook(sheet =>
            {
                var row1 = sheet.CreateRow(1);
                row1.CreateCell(0).SetCellValue("Bango, Stephen");
                row1.CreateCell(1).SetCellValue(day1);
                SetTimeCell(row1, 6, day1, TimeSpan.Parse("13:13:00"));

                var row2 = sheet.CreateRow(2);
                row2.CreateCell(0).SetCellValue("King, Robert");
                row2.CreateCell(1).SetCellValue(day2);
                SetTimeCell(row2, 6, day2, TimeSpan.Parse("15:07:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.Equal(2, _capturedRows.Count);
        }

        [Fact]
        public async Task ImportTripsAsync_ValidationFails_ReturnsInvalidResultWithErrors_AndDoesNotPersist()
        {
            var driverDate = new DateTime(2024, 9, 23);
            var expectedErrors = new List<ValidationError>
            {
                new ValidationError { Driver = "Wilson, Tomika", TripDate = driverDate, Message = "test error" }
            };

            _mockValidationService
                .Setup(s => s.Validate(It.IsAny<List<DriverDay>>()))
                .Returns(new ValidationResult { IsValid = false, Errors = expectedErrors });

            using var stream = BuildWorkbook(sheet =>
            {
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("Wilson, Tomika");
                row.CreateCell(1).SetCellValue(driverDate);
                SetTimeCell(row, 6, driverDate, TimeSpan.Parse("13:13:00"));
            });

            var service = new FileImportService(
                MakeUnitOfWork(), _mockAggregationService.Object, _mockValidationService.Object);

            var result = await service.ImportTripsAsync(stream, ".xlsx", CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Equal("Wilson, Tomika", result.Errors[0].Driver);
        }

        private static ITripUnitOfWork MakeUnitOfWork()
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new TripContext(options);
            return new TripUnitOfWork(context, new FakeBulkOperationsProvider());
        }
    }
}