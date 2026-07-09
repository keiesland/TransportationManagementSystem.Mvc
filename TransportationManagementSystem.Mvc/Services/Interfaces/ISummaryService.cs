using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.ViewModels;

namespace TransportationManagementSystem.Services.Interfaces
{
    public interface ISummaryService
    {
        Task SummarizeAndResetAsync(CancellationToken ct);
        Task<SummaryListViewModel> GetSummariesForListAsync(SummaryGridDTO values, ISession session, CancellationToken ct);
        Task<Summary> GetSummaryDetailsAsync(int id, CancellationToken ct);
        Task<RideDictionary> ApplyFilterAsync(string[] filter, bool clear, ISession session);
        Task UpdatePageSizeAsync(int pageSize, ISession session);
        Task<(byte[] fileBytes, string filename)> ExportAndClearAsync(CancellationToken ct);
    }
}
