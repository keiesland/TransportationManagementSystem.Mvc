using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;

namespace TransportationManagementSystem.Mvc.Services.Interfaces
{
    public interface IAggregationService
    {
        List<DriverDay> Aggregate(List<TripImportRow> rows);
    }
}
