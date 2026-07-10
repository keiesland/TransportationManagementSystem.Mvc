using System.Data;
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.Data.Query;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.UnitOfWork;
using TransportationManagementSystem.Mvc.Utilities;
using TransportationManagementSystem.Mvc.ViewModels;


namespace TransportationManagementSystem.Mvc.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly TripUnitOfWork _data;
        private readonly IExcelExportService _excelExportService;

        private static TimeSpan RoundToMinute(TimeSpan value) =>
              TimeSpan.FromMinutes(Math.Round(value.TotalMinutes));

        public SummaryService(TripContext context, IExcelExportService excelExportService)
        {
            _data = new TripUnitOfWork(context);
            _excelExportService = excelExportService;
        }

        /// <summary>
        /// Deletes all existing summaries and rebuilds summary data from trips
        /// </summary>
        public async Task SummarizeAndResetAsync(CancellationToken ct)
        {
            // Delete existing summaries if any exist
            if (_data.Summaries.Count() > 0)
            {
                await DeleteRecords.DeleteAllSummariesAsync(_data, ct);
            }

            // Load all required data
            var tripList = (await _data.Trips.ListAsync(new QueryOptions<Trip>
            {
                OrderBy = t => t.DriverId
            }, ct)).ToList();

            var driverList = (await _data.Drivers.ListAsync(new QueryOptions<Driver>
            {
                OrderBy = d => d.DriverId
            }, ct)).ToList();

            var tripDateList = (await _data.TripDates.ListAsync(new QueryOptions<TripDate>
            {
                OrderBy = t => t.Date
            }, ct)).ToList();

            // Summarize the data — calculation + batched bulk insert, fully async
            await SummarizeData.SummarizeDriverDataAsync(_data, tripList, driverList, tripDateList, ct);
        }

        /// <summary>
        /// Retrieves a paginated, sorted, and filtered list of summaries for the List view
        /// </summary>
        public async Task<SummaryListViewModel> GetSummariesForListAsync(SummaryGridDTO values, ISession session, CancellationToken ct)
        {
            // Get grid builder
            var builder = new SummaryGridBuilder(session, values,
                defaultSortField: nameof(Summary.Driver.FullName));

            // Build query options with includes, sorting, and paging
            var options = BuildSummaryQueryOptions(builder);

            // Load data asynchronously
            var summaries = await _data.Summaries.ListAsync(options, ct);
            var drivers = await _data.Drivers.ListAsync(new QueryOptions<Driver>
            {
                OrderBy = d => d.DriverId
            }, ct);
            var tripDates = await _data.TripDates.ListAsync(new QueryOptions<TripDate>
            {
                OrderBy = t => t.Date
            }, ct);

            // Create view model
            var vm = new SummaryListViewModel
            {
                Summaries = summaries,
                Drivers = drivers,
                TripDates = tripDates,
                CurrentRoute = builder.CurrentRoute,
                TotalPages = builder.GetTotalPages(_data.Summaries.Count(options))
            };

            return vm;
        }

        /// <summary>
        /// Retrieves a single summary with related data for the Details view
        /// </summary>
        public async Task<Summary> GetSummaryDetailsAsync(int id, CancellationToken ct)
        {
            var summary = await _data.Summaries.GetAsync(new QueryOptions<Summary>
            {
                Includes = "Driver, TripDate",
                Where = s => s.SummaryId == id
            }, ct);

            return summary;
        }

        /// <summary>
        /// Applies or clears filter criteria and saves to session
        /// </summary>
        public async Task<TripDictionary> ApplyFilterAsync(string[] filter, bool clear, ISession session)
        {
            var builder = new SummaryGridBuilder(session);

            if (clear)
            {
                builder.ClearFilterSegments();
            }
            else
            {
                builder.LoadFilterSegments(filter);
            }

            builder.SaveRouteSegments();

            await Task.CompletedTask; // Placeholder for consistency; remove if no async work needed

            return builder.CurrentRoute;
        }

        /// <summary>
        /// Updates page size preference and saves to session
        /// </summary>
        public async Task UpdatePageSizeAsync(int pageSize, ISession session)
        {
            var builder = new SummaryGridBuilder(session);
            builder.CurrentRoute.PageSize = pageSize;
            builder.SaveRouteSegments();

            await Task.CompletedTask; // Placeholder for consistency; remove if no async work needed
        }

        /// <summary>
        /// Exports all summaries to an Excel file and deletes all data from tables
        /// </summary>
        public async Task<(byte[] fileBytes, string filename)> ExportAndClearAsync(CancellationToken ct)
        {
            // Step 1: Build the Excel file in memory
            var fileBytes = await BuildExcelFileAsync(ct);

            // Step 2: Delete all table data now that we have the file
            await DeleteRecords.DeleteAllTableDataAsync(_data, ct);

            // Step 3: Build filename with current date
            var filename = "TransportManagementData_" + DateTime.Now.Date.ToShortDateString() + ".xlsx";

            return (fileBytes, filename);
        }

        // ========== Private Helper Methods ==========

        private SummaryQueryOptions BuildSummaryQueryOptions(SummaryGridBuilder builder)
        {
            var options = new SummaryQueryOptions
            {
                Includes = "Driver, TripDate",
                OrderByDirection = builder.CurrentRoute.SortDirection,
                PageNumber = builder.CurrentRoute.PageNumber,
                PageSize = builder.CurrentRoute.PageSize
            };

            options.SortFilter(builder);

            return options;
        }

        /// <summary>
        /// Builds the Summaries DataTable and delegates workbook creation to IExcelExportService
        /// </summary>
        private async Task<byte[]> BuildExcelFileAsync(CancellationToken ct)
        {
            // Create DataTable with column structure
            DataTable dt = new DataTable("Summaries");
            dt.Columns.AddRange(new DataColumn[14]
            {
                new DataColumn("Driver"),
                new DataColumn("Date"),
                new DataColumn("Start"),
                new DataColumn("Out1"),
                new DataColumn("In1"),
                new DataColumn("Out2"),
                new DataColumn("In2"),
                new DataColumn("Out3"),
                new DataColumn("In3"),
                new DataColumn("Out4"),
                new DataColumn("In4"),
                new DataColumn("End"),
                new DataColumn("ActualTime"),
                new DataColumn("WeeklyTime")
            });

            // Load summaries with related data via the repository
            var summaries = await _data.Summaries.ListAsync(new QueryOptions<Summary>
            {
                Includes = "Driver, TripDate"
            }, ct);

            // Populate DataTable rows
            foreach (var summary in summaries)
            {
                dt.Rows.Add(
                    summary.Driver.FullName,
                    summary.TripDate.Date,
                    summary.Start,
                    summary.Out1,
                    summary.In1,
                    summary.Out2,
                    summary.In2,
                    summary.Out3,
                    summary.In3,
                    summary.Out4,
                    summary.In4,
                    summary.End,
                    summary.ActualTime,
                    summary.WeeklyTime);
            }

            // Hand the DataTable off to the reusable Excel export service
            return await _excelExportService.BuildWorkbookAsync(dt, ct);
        }
    }
}
