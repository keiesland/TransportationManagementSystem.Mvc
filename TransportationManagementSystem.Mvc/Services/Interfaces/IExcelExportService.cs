using System.Data;

namespace TransportationManagementSystem.Services.Interfaces
{
    public interface IExcelExportService
    {
        /// <summary>
        /// Builds an Excel workbook in memory from a DataTable and returns it as a byte array.
        /// </summary>
        Task<byte[]> BuildWorkbookAsync(DataTable data, CancellationToken ct = default);

        /// <summary>
        /// Builds an Excel workbook containing multiple sheets, one per DataTable.
        /// Each DataTable's TableName is used as its sheet name.
        /// </summary>
        Task<byte[]> BuildWorkbookAsync(DataTable[] sheets, CancellationToken ct = default);

    }
}
