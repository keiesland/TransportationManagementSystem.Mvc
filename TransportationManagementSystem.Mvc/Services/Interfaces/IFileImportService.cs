using TransportationManagementSystem.Mvc.Data.DTOs;

namespace TransportationManagementSystem.Mvc.Services.Interfaces
{
    public interface IFileImportService
    {
        Task<ImportResult> ImportTripsAsync(Stream fileStream, string fileExtension, CancellationToken ct);
    }
}
