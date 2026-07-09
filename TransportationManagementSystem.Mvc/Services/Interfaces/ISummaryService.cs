using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.ViewModels;

namespace TransportationManagementSystem.Mvc.Services.Interfaces
{
    public interface ISummaryService
    {
        SummaryResult Summarize(List<DriverDay> driverDays);
        Task SummarizeAndResetAsync(CancellationToken ct);
        Task<SummaryListViewModel> GetSummariesForListAsync(SummaryGridDTO values, ISession session, CancellationToken ct);
        Task<Summary> GetSummaryDetailsAsync(int id, CancellationToken ct);
        Task<TripDictionary> ApplyFilterAsync(string[] filter, bool clear, ISession session);
        Task UpdatePageSizeAsync(int pageSize, ISession session);
        Task<(byte[] fileBytes, string filename)> ExportAndClearAsync(CancellationToken ct);
    }
}
