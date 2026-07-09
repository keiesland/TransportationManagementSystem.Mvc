using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Services;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Services
{

    public class SummaryServiceTests
    {
        private readonly Mock<IExcelExportService> _excelMock;

        public SummaryServiceTests()
        {
            _excelMock = new Mock<IExcelExportService>();
            SQLitePCL.Batteries.Init();
        }

        private TripContext BuildContext()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TripContext>()
                .UseSqlite(connection)
                .Options;

            var context = new TripContext(options);

            context.Database.EnsureCreated();

            return context;
        }


        private SummaryService BuildService(TripContext context)
        {
            return new SummaryService(context, _excelMock.Object);
        }

        private static Driver MakeDriver(int driverId, string lastName, string firstName, string fullName)
        {
            return new Driver { DriverId = driverId, LastName = lastName, FirstName = firstName, FullName = fullName };
        }

        private static TripDate MakeTripDate(int tripDateId, DateTime date, int weekNumber)
        {
            return new TripDate { TripDateId = tripDateId, Date = date, WeekNumber = weekNumber };
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

        private static Mock<ISession> BuildSession()
        {
            return new Mock<ISession>();
        }

        [Fact]
        public async Task GetSummaryDetailsAsync_ReturnsSummary_WhenSummaryExists()
        {
            var context = BuildContext();

            var driver = MakeDriver(1, "Smith", "James", "James Smith");
            var tripDate = MakeTripDate(1, new DateTime(2026, 6, 1), 22);
            var summary = MakeSummary(1, driver, tripDate);

            context.Drivers.Add(driver);
            context.TripDates.Add(tripDate);
            context.Summaries.Add(summary);

            await context.SaveChangesAsync();

            var service = BuildService(context);

            var result = await service.GetSummaryDetailsAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(driver.FullName, result.Driver.FullName);
        }

        [Fact]
        public async Task GetSummaryDetailsAsync_ReturnsNull_WhenSummaryDoesNotExist()
        {
            var context = BuildContext();

            var service = BuildService(context);

            var result = await service.GetSummaryDetailsAsync(99, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdatePageSizeAsync_CompletesSuccessfully()
        {
            var context = BuildContext();
            var session = BuildSession();

            var service = BuildService(context);

            await service.UpdatePageSizeAsync(50, session.Object);

            session.Verify(s => s.Set(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task ApplyFilterAsync_WithClearTrue_Completes()
        {
            var context = BuildContext();
            var session = BuildSession();

            var service = BuildService(context);

            await service.ApplyFilterAsync(
                Array.Empty<string>(),
                true,
                session.Object);

            session.Verify(s => s.Set(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task ApplyFilterAsync_LoadsFilterSegments()
        {
            var context = BuildContext();
            var session = BuildSession();

            var service = BuildService(context);

            await service.ApplyFilterAsync(
                new[] { "Driver", "John Smith" },
                false,
                session.Object);

            session.Verify(s => s.Set(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>()),
                Times.AtLeastOnce());
        }

        [Fact]
        public async Task ExportAndClearAsync_ReturnsWorkbookBytes()
        {
            var context = BuildContext();

            var driver = MakeDriver(1, "Smith", "James", "James Smith");
            var tripDate = MakeTripDate(1, new DateTime(2026, 6, 1), 22);

            context.Drivers.Add(driver);
            context.TripDates.Add(tripDate);

            context.Summaries.Add(MakeSummary(1, driver, tripDate));

            await context.SaveChangesAsync();

            _excelMock.Setup(e =>
                    e.BuildWorkbookAsync(
                        It.IsAny<DataTable>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            var service = BuildService(context);

            var result = await service.ExportAndClearAsync(CancellationToken.None);

            Assert.Equal(new byte[] { 1, 2, 3 }, result.fileBytes);
        }

        [Fact]
        public async Task ExportAndClearAsync_DeletesAllData()
        {
            var context = BuildContext();

            context.Drivers.Add(MakeDriver(1, "Smith", "James", "James Smith"));
            await context.SaveChangesAsync();

            _excelMock.Setup(e =>
                    e.BuildWorkbookAsync(
                        It.IsAny<DataTable>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<byte>());

            var service = BuildService(context);

            await service.ExportAndClearAsync(CancellationToken.None);

            Assert.Empty(context.Drivers);
        }

        [Fact]
        public async Task ExportAndClearAsync_ReturnsFilename()
        {
            var context = BuildContext();

            _excelMock.Setup(e =>
                    e.BuildWorkbookAsync(
                        It.IsAny<DataTable>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<byte>());

            var service = BuildService(context);

            var result = await service.ExportAndClearAsync(CancellationToken.None);

            Assert.StartsWith("TransportManagementData_", result.filename);
            Assert.EndsWith(".xlsx", result.filename);
        }

        [Fact]
        public async Task GetSummariesForListAsync_ReturnsViewModel()
        {
            var context = BuildContext();

            var driver = MakeDriver(1, "Smith", "James", "James Smith");
            var tripDate = MakeTripDate(1, new DateTime(2026, 6, 1), 22);

            context.Drivers.Add(driver);
            context.TripDates.Add(tripDate);
            context.Summaries.Add(MakeSummary(1, driver, tripDate));

            await context.SaveChangesAsync();

            var service = BuildService(context);

            var dto = new SummaryGridDTO();

            var session = BuildSession();

            var vm = await service.GetSummariesForListAsync(
                dto,
                session.Object,
                CancellationToken.None);

            Assert.NotNull(vm);
            Assert.Single(vm.Summaries);
            Assert.Single(vm.Drivers);
            Assert.Single(vm.TripDates);
        }
    }
}