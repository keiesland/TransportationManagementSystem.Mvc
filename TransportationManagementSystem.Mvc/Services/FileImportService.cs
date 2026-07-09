using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Data.Query;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.UnitOfWork;
using TransportationManagementSystem.Mvc.Utilities;

namespace TransportationManagementSystem.Mvc.Services
{
    public class FileImportService : IFileImportService
    {
        private readonly ITripUnitOfWork _data;
        private readonly IAggregationService _aggregationService;
        private readonly IValidationService _validationService;

        public FileImportService(
            ITripUnitOfWork data,
            IAggregationService aggregationService,
            IValidationService validationService)
        {
            _data = data;
            _aggregationService = aggregationService;
            _validationService = validationService;
        }

        public async Task<ImportResult> ImportTripsAsync(Stream fileStream, string fileExtension, CancellationToken ct)
        {
            // Parse only -- no database writes yet.
            ISheet sheet = fileExtension == ".xls"
                ? (ISheet)new HSSFWorkbook(fileStream).GetSheetAt(0)
                : new XSSFWorkbook(fileStream).GetSheetAt(0);

            var rows = ParseRowsFromSheet(sheet);

            var driverDays = _aggregationService.Aggregate(rows);
            var validationResult = _validationService.Validate(driverDays);

            if (!validationResult.IsValid)
            {
                return new ImportResult
                {
                    IsValid = false,
                    ImportedCount = 0,
                    Errors = validationResult.Errors
                };
            }

            // Validation passed -- now it's safe to touch the database.
            var importedCount = await PersistRowsAsync(rows, ct);

            return new ImportResult
            {
                IsValid = true,
                ImportedCount = importedCount
            };
        }

        // ========== Private Helper Methods ==========

        /// <summary>
        /// Parses trip rows from the worksheet into plain DTOs -- no database
        /// access, no Driver/TripDate creation. Safe to run before validation.
        /// </summary>
        private List<TripImportRow> ParseRowsFromSheet(ISheet sheet)
        {
            var rows = new List<TripImportRow>();

            for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
            {
                IRow row = sheet.GetRow(i);
                if (row == null) continue;
                if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;

                var pickupArrival = GetTimeSpan(row, 4);
                var actualPickup = GetTimeSpan(row, 5);
                var actualDropoff = GetTimeSpan(row, 6);
                if (pickupArrival == null && actualPickup == null && actualDropoff == null)
                    continue;

                var driverName = row.GetCell(0)?.ToString()?.Trim();
                if (string.IsNullOrEmpty(driverName)) continue;

                var tripDate = row.GetCell(1).DateCellValue.Date;

                rows.Add(new TripImportRow
                {
                    Driver = driverName,
                    TripDate = tripDate,
                    WeekNumber = System.Globalization.ISOWeek.GetWeekOfYear(tripDate),
                    TripActualStartTime = GetTimeSpan(row, 2) ?? TimeSpan.Zero,
                    ScheduledPickupTime = GetTimeSpan(row, 3) ?? TimeSpan.Zero,
                    PickupArrivalTime = pickupArrival ?? TimeSpan.Zero,
                    ActualPickupTime = actualPickup ?? TimeSpan.Zero,
                    ActualDropoffTime = actualDropoff ?? TimeSpan.Zero,
                    ScheduledDropoffTime = GetTimeSpan(row, 7) ?? TimeSpan.Zero,
                    TripActualEndTime = GetTimeSpan(row, 8) ?? TimeSpan.Zero
                });
            }

            return rows;
        }

        /// <summary>
        /// Only called after validation passes. Deletes existing trips, resolves
        /// (or creates) Driver/TripDate rows, and bulk-inserts the new trips.
        /// </summary>
        private async Task<int> PersistRowsAsync(List<TripImportRow> rows, CancellationToken ct)
        {
            await DeleteRecords.DeleteAllTripsAsync(_data, ct);

            var existingDrivers = (await _data.Drivers.ListAsync(new QueryOptions<Driver>(), ct))
                .ToDictionary(d => d.FullName);
            var existingDates = (await _data.TripDates.ListAsync(new QueryOptions<TripDate>(), ct))
                .ToDictionary(d => d.Date.Date);

            var tripList = new List<Trip>();

            foreach (var row in rows)
            {
                var driver = await GetOrCreateDriverAsync(row.Driver, existingDrivers, ct);
                var tripDateEntity = await GetOrCreateTripDateAsync(row.TripDate, existingDates, ct);

                tripList.Add(new Trip
                {
                    DriverId = driver.DriverId,
                    TripDateId = tripDateEntity.TripDateId,
                    TripActualStart = row.TripActualStartTime,
                    ScheduledPickup = row.ScheduledPickupTime,
                    PickupArrival = row.PickupArrivalTime,
                    ActualPickup = row.ActualPickupTime,
                    ActualDropoff = row.ActualDropoffTime,
                    ScheduledDropoff = row.ScheduledDropoffTime,
                    TripActualEnd = row.TripActualEndTime
                });
            }

            await _data.Trips.BulkInsertAsync(tripList, ct);
            return tripList.Count;
        }

        private async Task<Driver> GetOrCreateDriverAsync(
            string driverName, Dictionary<string, Driver> existingDrivers, CancellationToken ct)
        {
            if (existingDrivers.TryGetValue(driverName, out var driver))
                return driver;

            var names = driverName.Split(", ");
            var newDriver = new Driver
            {
                FullName = driverName,
                LastName = names.Length > 0 ? names[0] : driverName,
                FirstName = names.Length > 1 ? names[1] : string.Empty
            };

            _data.Drivers.Insert(newDriver);
            await _data.SaveAsync(ct);

            existingDrivers[driverName] = newDriver;
            return newDriver;
        }

        private async Task<TripDate> GetOrCreateTripDateAsync(
            DateTime tripDate, Dictionary<DateTime, TripDate> existingDates, CancellationToken ct)
        {
            if (existingDates.TryGetValue(tripDate, out var tripDateEntity))
                return tripDateEntity;

            int weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(tripDate);
            var newDate = new TripDate { Date = tripDate, WeekNumber = weekNumber };
            _data.TripDates.Insert(newDate);
            await _data.SaveAsync(ct);

            existingDates[tripDate] = newDate;
            return newDate;
        }

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
                        return parsed == TimeSpan.Zero ? null : parsed;
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
