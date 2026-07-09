using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Data.Grid;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.ViewModels;

namespace TransportationManagementSystem.Mvc.Services.Interfaces
{
    public interface ITripService
    {
        TripListViewModel GetTripsForList(TripGridDTO values, ISession session);
        Trip GetTripDetails(int id);
        TripDictionary ApplyFilter(string[] filter, bool clear, ISession session);
        void UpdatePageSize(int pageSize, ISession session);
        Task ClearAllDataAsync(CancellationToken ct);
    }
}
