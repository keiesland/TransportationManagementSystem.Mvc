using System.Data;
using ClosedXML.Excel;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public async Task<byte[]> BuildWorkbookAsync(DataTable data, CancellationToken ct = default)
        {
            return await BuildWorkbookAsync(new[] { data }, ct);
        }

        public async Task<byte[]> BuildWorkbookAsync(DataTable[] sheets, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            using (var wb = new XLWorkbook())
            {
                foreach (var sheet in sheets)
                {
                    wb.Worksheets.Add(sheet);
                }

                using (var stream = new MemoryStream())
                {
                    wb.SaveAs(stream);

                    // ClosedXML's SaveAs has no async overload, so this method is
                    // sync under the hood — but it returns Task to keep the
                    // interface consistent and future-proof if that ever changes.
                    return await Task.FromResult(stream.ToArray());
                }
            }
        }
    }

}
