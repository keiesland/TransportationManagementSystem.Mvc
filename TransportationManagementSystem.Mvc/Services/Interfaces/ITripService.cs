using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.ViewModels;

namespace TransportationManagementSystem.Services.Interfaces
{
    public interface ITripService
    {
        TripListViewModel GetTripsForList(TripGridDTO values, ISession session);
        Trip GetTripDetails(int id);
        RideDictionary ApplyFilter(string[] filter, bool clear, ISession session);
        void UpdatePageSize(int pageSize, ISession session);
        Task ClearAllDataAsync(CancellationToken ct);
    }
}
