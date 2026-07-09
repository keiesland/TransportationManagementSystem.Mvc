using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;

namespace TransportationManagementSystem.UnitOfWork
{
    public interface ITripUnitOfWork
    {
        Repository<Trip> Trips { get; }
        Repository<Driver> Drivers { get; }
        Repository<TripDate> TripDates { get; }
        Repository<Summary> Summaries { get; }

        void Save();

        // add this below Save()
        Task SaveAsync(CancellationToken ct = default);
    }

}
