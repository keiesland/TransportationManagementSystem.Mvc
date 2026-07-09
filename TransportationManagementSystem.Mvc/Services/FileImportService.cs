using EFCore.BulkExtensions;
using TransportationManagementSystem.UtilityClasses;
using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Services.Interfaces;
using TransportationManagementSystem.UtilityClasses;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TransportationManagementSystem.UnitOfWork;

namespace TransportationManagementSystem.Services
{
    public class FileImportService : IFileImportService
    {
        private readonly TripUnitOfWork _data;

        public FileImportService(TripContext context)
        {
            _data = new TripUnitOfWork(context);
        }

        /// <summary>
        /// Imports trips from an Excel file stream, clearing existing trip data first.
        /// Returns the number of trips imported.
        /// </summary>
        public async Task<int> ImportTripsAsync(Stream fileStream, string fileExtension, CancellationToken ct)
        {
            // Clear existing trips before import
            await DeleteRecords.DeleteAllTripsAsync(_data, ct);

            // Load existing drivers and dates into memory ONCE before the loop
            var existingDrivers = (await _data.Drivers.ListAsync(
                    new QueryOptions<Driver>(), ct))
                .ToDictionary(d => d.FullName);

            var existingDates = (await _data.TripDates.ListAsync(
                    new QueryOptions<TripDate>(), ct))
                .ToDictionary(d => d.Date.Date);

            // Parse the worksheet — NPOI's parse itself is sync (no async API), but
            // everything that touches the database below it is fully async
            ISheet sheet = fileExtension == ".xls"
                ? (ISheet)new HSSFWorkbook(fileStream).GetSheetAt(0)
                : new XSSFWorkbook(fileStream).GetSheetAt(0);

            var tripList = await ParseTripsFromSheetAsync(sheet, existingDrivers, existingDates, ct);

            // Bulk insert all parsed trips
            await BulkInsertTripsAsync(tripList, ct);

            return tripList.Count;
        }

        // ========== Private Helper Methods ==========

        /// <summary>
        /// Parses trip rows from the worksheet, creating new Driver/TripDate records as needed
        /// </summary>
        private async Task<List<Trip>> ParseTripsFromSheetAsync(
            ISheet sheet,
            Dictionary<string, Driver> existingDrivers,
            Dictionary<DateTime, TripDate> existingDates,
            CancellationToken ct)
        {
            var tripList = new List<Trip>();

            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
            {
                ct.ThrowIfCancellationRequested();

                IRow row = sheet.GetRow(i);
                if (row == null) continue;
                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;

                // Safely read times — skip row if critical cells are missing
                var pickupArrival = GetTimeSpan(row, 4);
                var actualPickup = GetTimeSpan(row, 5);
                var actualDropoff = GetTimeSpan(row, 6);
                if (pickupArrival == null && actualPickup == null && actualDropoff == null)
                    continue;

                var driverName = row.GetCell(0)?.ToString()?.Trim();
                if (string.IsNullOrEmpty(driverName)) continue;

                var driver = await GetOrCreateDriverAsync(driverName, existingDrivers, ct);
                var tripDate = row.GetCell(1).DateCellValue.Date;
                var tripDateEntity = await GetOrCreateTripDateAsync(tripDate, existingDates, ct);

                tripList.Add(new Trip
                {
                    DriverId = driver.DriverId,
                    TripDateId = tripDateEntity.TripDateId,
                    TripActualStart = GetTimeSpan(row, 2) ?? TimeSpan.Zero,
                    ScheduledPickup = GetTimeSpan(row, 3) ?? TimeSpan.Zero,
                    PickupArrival = pickupArrival ?? TimeSpan.Zero,
                    ActualPickup = actualPickup ?? TimeSpan.Zero,
                    ActualDropoff = actualDropoff ?? TimeSpan.Zero,
                    ScheduledDropoff = GetTimeSpan(row, 7) ?? TimeSpan.Zero,
                    TripActualEnd = GetTimeSpan(row, 8) ?? TimeSpan.Zero
                });
            }

            return tripList;
        }

        /// <summary>
        /// Looks up driver in cache dictionary, or creates and persists a new one
        /// </summary>
        private async Task<Driver> GetOrCreateDriverAsync(
            string driverName,
            Dictionary<string, Driver> existingDrivers,
            CancellationToken ct)
        {
            if (existingDrivers.TryGetValue(driverName, out var driver))
            {
                return driver;
            }

            var names = driverName.Split(", ");
            var newDriver = new Driver
            {
                FullName = driverName,
                LastName = names.Length > 0 ? names[0] : driverName,
                FirstName = names.Length > 1 ? names[1] : string.Empty
            };

            _data.Drivers.Insert(newDriver);
            await _data.SaveAsync(ct); // Needed immediately to get the generated DriverId

            existingDrivers[driverName] = newDriver;
            return newDriver;
        }

        /// <summary>
        /// Looks up trip date in cache dictionary, or creates and persists a new one
        /// </summary>
        private async Task<TripDate> GetOrCreateTripDateAsync(
            DateTime tripDate,
            Dictionary<DateTime, TripDate> existingDates,
            CancellationToken ct)
        {
            if (existingDates.TryGetValue(tripDate, out var tripDateEntity))
            {
                return tripDateEntity;
            }

            // Calculate week number from date — don't trust the Excel formula
            int weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(tripDate);

            var newDate = new TripDate { Date = tripDate, WeekNumber = weekNumber };
            _data.TripDates.Insert(newDate);
            await _data.SaveAsync(ct);

            existingDates[tripDate] = newDate;
            return newDate;
        }

        /// <summary>
        /// Bulk inserts the parsed trip list using the repository's bulk operation
        /// </summary>
        private async Task BulkInsertTripsAsync(List<Trip> tripList, CancellationToken ct)
        {
            await _data.Trips.BulkInsertAsync(tripList, ct);
        }

        /// <summary>
        /// Safely reads a TimeSpan from a cell — returns null instead of throwing on bad cells.
        /// Handles both native Excel time/numeric cells AND text cells containing a time
        /// string like "13:00:00" (some source files store time columns as text rather
        /// than native Excel time values).
        /// </summary>
        private TimeSpan? GetTimeSpan(IRow row, int cellIndex)
        {
            try
            {
                var cell = row.GetCell(cellIndex);
                if (cell == null || cell.CellType == CellType.Blank) return null;

                if (cell.CellType == CellType.String)
                {
                    var text = cell.StringCellValue?.Trim();
                    if (string.IsNullOrEmpty(text)) return null;

                    if (TimeSpan.TryParse(text, out var parsed))
                    {
                        return parsed == TimeSpan.Zero ? null : parsed;
                    }

                    return null;
                }

                if (cell.NumericCellValue == 0.00) return null;
                return cell.DateCellValue.TimeOfDay;
            }
            catch
            {
                return null;
            }
        }
    }



}
