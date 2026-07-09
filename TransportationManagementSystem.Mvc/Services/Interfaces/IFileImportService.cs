namespace TransportationManagementSystem.Services.Interfaces
{
    public interface IFileImportService
    {
        Task<int> ImportTripsAsync(Stream fileStream, string fileExtension, CancellationToken ct);
    }
}
