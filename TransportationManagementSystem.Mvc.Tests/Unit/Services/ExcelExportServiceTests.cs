using System.Data;
using ClosedXML.Excel;
using TransportationManagementSystem.Mvc.Services;

namespace TransportationManagementSystem.Mvc.Tests.Unit.Services
{
    public class ExcelExportServiceTests
    {
        private readonly ExcelExportService _service = new();

        private static DataTable MakeTable(string tableName, params (string Column, object Value)[][] rows)
        {
            var table = new DataTable(tableName);

            if (rows.Length > 0)
            {
                foreach (var col in rows[0])
                {
                    table.Columns.Add(col.Column, col.Value?.GetType() ?? typeof(string));
                }

                foreach (var row in rows)
                {
                    var dataRow = table.NewRow();
                    foreach (var col in row)
                    {
                        dataRow[col.Column] = col.Value;
                    }
                    table.Rows.Add(dataRow);
                }
            }

            return table;
        }

        [Fact]
        public async Task BuildWorkbookAsync_SingleTable_ReturnsNonEmptyByteArray()
        {
            var table = MakeTable("Summary",
                new[] { ("Driver", (object)"Bango, Stephen"), ("PaidTime", (object)"04:16") });

            var bytes = await _service.BuildWorkbookAsync(table);

            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public async Task BuildWorkbookAsync_SingleTable_ProducesReadableWorkbookWithCorrectSheetName()
        {
            var table = MakeTable("Summary",
                new[] { ("Driver", (object)"Bango, Stephen") });

            var bytes = await _service.BuildWorkbookAsync(table);

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);

            Assert.Single(workbook.Worksheets);
            Assert.Equal("Summary", workbook.Worksheets.First().Name);
        }

        [Fact]
        public async Task BuildWorkbookAsync_SingleTable_PreservesHeaderAndRowData()
        {
            var table = MakeTable("Summary",
                new[] { ("Driver", (object)"Bango, Stephen"), ("PaidTime", (object)"04:16") },
                new[] { ("Driver", (object)"King, Robert"), ("PaidTime", (object)"05:53") });

            var bytes = await _service.BuildWorkbookAsync(table);

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            // Row 1 = headers, rows 2-3 = data (ClosedXML's DataTable insert
            // includes the header row by default).
            Assert.Equal("Driver", ws.Cell(1, 1).GetString());
            Assert.Equal("PaidTime", ws.Cell(1, 2).GetString());
            Assert.Equal("Bango, Stephen", ws.Cell(2, 1).GetString());
            Assert.Equal("04:16", ws.Cell(2, 2).GetString());
            Assert.Equal("King, Robert", ws.Cell(3, 1).GetString());
            Assert.Equal("05:53", ws.Cell(3, 2).GetString());
        }

        [Fact]
        public async Task BuildWorkbookAsync_MultipleTables_CreatesOneSheetPerTable()
        {
            var summarySheet = MakeTable("Summary",
                new[] { ("Driver", (object)"Bango, Stephen") });
            var tripSheet = MakeTable("Trips",
                new[] { ("Driver", (object)"Bango, Stephen"), ("TripId", (object)1) });

            var bytes = await _service.BuildWorkbookAsync(new[] { summarySheet, tripSheet });

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);

            Assert.Equal(2, workbook.Worksheets.Count);
            Assert.Contains(workbook.Worksheets, ws => ws.Name == "Summary");
            Assert.Contains(workbook.Worksheets, ws => ws.Name == "Trips");
        }

        [Fact]
        public async Task BuildWorkbookAsync_MultipleTables_PreservesSheetOrder()
        {
            var firstSheet = MakeTable("First", new[] { ("Col", (object)"A") });
            var secondSheet = MakeTable("Second", new[] { ("Col", (object)"B") });

            var bytes = await _service.BuildWorkbookAsync(new[] { firstSheet, secondSheet });

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);

            Assert.Equal("First", workbook.Worksheets.ElementAt(0).Name);
            Assert.Equal("Second", workbook.Worksheets.ElementAt(1).Name);
        }

        [Fact]
        public async Task BuildWorkbookAsync_EmptyDataTable_StillProducesValidWorkbookWithHeaderOnly()
        {
            var table = new DataTable("Empty");
            table.Columns.Add("Driver");

            var bytes = await _service.BuildWorkbookAsync(table);

            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();

            Assert.Equal("Driver", ws.Cell(1, 1).GetString());
            Assert.True(ws.Cell(2, 1).IsEmpty());
        }

        [Fact]
        public async Task BuildWorkbookAsync_CancellationRequested_ThrowsOperationCanceledException()
        {
            var table = MakeTable("Summary", new[] { ("Driver", (object)"Bango, Stephen") });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _service.BuildWorkbookAsync(table, cts.Token));
        }

        [Fact]
        public async Task BuildWorkbookAsync_SingleTableOverload_DelegatesToArrayOverload()
        {
            // Confirms the single-DataTable convenience overload produces
            // the same result as calling the array overload directly.
            var table = MakeTable("Summary", new[] { ("Driver", (object)"Bango, Stephen") });

            var singleResult = await _service.BuildWorkbookAsync(table);
            var arrayResult = await _service.BuildWorkbookAsync(new[] { table });

            using var singleWb = new XLWorkbook(new MemoryStream(singleResult));
            using var arrayWb = new XLWorkbook(new MemoryStream(arrayResult));

            Assert.Equal(singleWb.Worksheets.Count, arrayWb.Worksheets.Count);
            Assert.Equal(singleWb.Worksheets.First().Name, arrayWb.Worksheets.First().Name);
        }
    }
}
