using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories.Interfaces;
using TransportationManagementSystem.Services;
using TransportationManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;


namespace TransportationManagementSystem.Tests.Unit.Services
{
    public class TripServiceTests : IDisposable
    {
        private readonly TripContext _db;
        private readonly TripService _tripService;
        private readonly TestSession _session;
        private readonly Mock<IBulkOperationsProvider> _mockBulkOps;

        public TripServiceTests()
        {
            var options = new DbContextOptionsBuilder<TripContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique per test instance
                .Options;

            _db = new TripContext(options);
            _session = new TestSession();

            // A mocked bulk-ops provider is included for every test, even
            // though most don't touch it — this avoids EVER hitting the real
            // EFCore.BulkExtensions path against InMemory, which doesn't
            // support relational-specific operations (same issue we hit with
            // SQLite yesterday, now showing up against InMemory instead).
            _mockBulkOps = new Mock<IBulkOperationsProvider>();
            _tripService = new TripService(_db, _mockBulkOps.Object);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        // ---------- GetTripsForList ----------

        [Fact]
        public void GetTripsForList_WithNullSortField_FallsBackToDefaultSortField()
        {
            // Arrange: a TripGridDTO with SortField left null — the one
            // property on GridDTO that has no safe default initializer.
            // We traced through TripGridBuilder -> GridBuilder by hand and
            // confirmed `routes.SortField = values.SortField ?? defaultSortField;`
            // handles this — this test locks that behavior in so a future
            // change can't silently remove the fallback without a test failing.
            var values = new TripGridDTO
            {
                SortField = null
                // Driver/TripDate/PageNumber/PageSize/SortDirection all keep
                // their safe defaults from GridDTO/TripGridDTO
            };

            // Act
            var vm = _tripService.GetTripsForList(values, _session);

            // Assert: SortField should have fallen back to the default
            // passed into TripGridBuilder inside GetTripsForList —
            // nameof(Trip.Driver.FullName), which evaluates to "FullName".
            Assert.Equal(nameof(Trip.Driver.FullName), vm.CurrentRoute.SortField);
        }

        [Fact]
        public void GetTripsForList_WithExplicitSortField_DoesNotOverrideIt()
        {
            // Companion test: confirms the fallback ONLY kicks in when
            // SortField is actually null — an explicitly provided value
            // should be respected, not silently replaced.
            var values = new TripGridDTO
            {
                SortField = "TripActualStart"
            };

            var vm = _tripService.GetTripsForList(values, _session);

            Assert.Equal("TripActualStart", vm.CurrentRoute.SortField);
        }

        // ---------- ApplyFilter ----------

        [Fact]
        public void ApplyFilter_ClearTrue_ResetsBothFiltersToDefault()
        {
            // Act
            _tripService.ApplyFilter(filter: null, clear: true, _session);

            // Assert: re-read the saved state the same way the NEXT real
            // request would — by constructing a fresh TripGridBuilder
            // against the same session — rather than reaching into any
            // protected/internal members directly.
            var reloaded = new TripGridBuilder(_session);

            Assert.Equal(TripGridDTO.DefaultFilter, reloaded.CurrentRoute.DriverFilter);
            Assert.Equal(TripGridDTO.DefaultFilter, reloaded.CurrentRoute.TripDateFilter);
        }

        [Fact]
        public void ApplyFilter_ClearFalse_SavesProvidedFilterValuesWithoutPrefix()
        {
            var filter = new[] { "Smith", "2026-06-01" };

            // Act
            _tripService.ApplyFilter(filter, clear: false, _session);

            // Assert: LoadFilterSegments stores values WITH the "driver-"/"date-"
            // prefix internally, but RideDictionary's DriverFilter/TripDateFilter
            // GETTERS strip the prefix back off — so the reloaded values should
            // be the plain, unprefixed strings we provided.
            var reloaded = new TripGridBuilder(_session);

            Assert.Equal("Smith", reloaded.CurrentRoute.DriverFilter);
            Assert.Equal("2026-06-01", reloaded.CurrentRoute.TripDateFilter);
        }

        [Fact]
        public void ApplyFilter_ClearFalse_ThenClearTrue_OverridesBackToDefault()
        {
            // Companion test: confirms calling ApplyFilter again with
            // clear=true correctly overrides a previously-saved filter,
            // rather than the two calls somehow conflicting.
            _tripService.ApplyFilter(new[] { "Smith", "2026-06-01" }, clear: false, _session);
            _tripService.ApplyFilter(filter: null, clear: true, _session);

            var reloaded = new TripGridBuilder(_session);

            Assert.Equal(TripGridDTO.DefaultFilter, reloaded.CurrentRoute.DriverFilter);
            Assert.Equal(TripGridDTO.DefaultFilter, reloaded.CurrentRoute.TripDateFilter);
        }
        // ---------- ClearAllDataAsync ----------

        [Fact]
        public async Task ClearAllDataAsync_TableHasTrips_CallsBulkDeleteWithSeededTrip()
        {
            // Arrange: seed one trip. Adding it via _db.Trips.Add(trip) with
            // trip.TripDate set as a navigation property means EF Core's
            // change tracker will also insert the related TripDate
            // automatically on SaveChanges — both InMemory operations,
            // perfectly normal, no bulk extensions involved here.
            var tripDate = new TripDate { TripDateId = 1, Date = new DateTime(2026, 6, 1), WeekNumber = 22 };
            var trip = new Trip
            {
                TripId = 1,
                DriverId = 1,
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

            _db.Trips.Add(trip);
            await _db.SaveChangesAsync();

            // Act
            await _tripService.ClearAllDataAsync(CancellationToken.None);

            // Assert: BulkDeleteAllAsync's AnyAsync() check works fine against
            // InMemory (plain LINQ, nothing relational-specific) — so it
            // correctly detects Trips has 1 row and TripDates has 1 row, and
            // calls the (mocked) bulk-delete provider for each.
            _mockBulkOps.Verify(
                p => p.BulkDeleteAsync(
                    _db,
                    It.Is<System.Collections.Generic.IEnumerable<Trip>>(list => list.Count() == 1),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockBulkOps.Verify(
                p => p.BulkDeleteAsync(
                    _db,
                    It.IsAny<System.Collections.Generic.IEnumerable<TripDate>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ClearAllDataAsync_EmptyTables_NeverCallsBulkDelete()
        {
            // Regression-style test: with nothing seeded, every repository's
            // AnyAsync() check should short-circuit BEFORE ever calling the
            // bulk-delete provider — confirming DeleteAllTableDataAsync
            // doesn't blindly call bulk delete on empty tables.

            // Act
            await _tripService.ClearAllDataAsync(CancellationToken.None);

            // Assert
            _mockBulkOps.Verify(
                p => p.BulkDeleteAsync(
                    It.IsAny<DbContext>(),
                    It.IsAny<System.Collections.Generic.IEnumerable<Trip>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---------- UpdatePageSize ----------

        [Fact]
        public void UpdatePageSize_ChangePageSize_ReturnsUpdatedPageSize()
        {
            var pageSize = 3;

            // Act
            _tripService.UpdatePageSize(pageSize, _session);

            // Assert: re-read the saved state the same way the NEXT real
            // request would — by constructing a fresh TripGridBuilder
            // against the same session — rather than reaching into any
            // protected/internal members directly.
            var reloaded = new TripGridBuilder(_session);

            Assert.Equal(pageSize, reloaded.CurrentRoute.PageSize);
        }

        //public void UpdatePageSize(int pageSize, ISession session)
        //{
        //    var builder = new TripGridBuilder(session);
        //    builder.CurrentRoute.PageSize = pageSize;
        //    builder.SaveRouteSegments();
        //}
    }
}

