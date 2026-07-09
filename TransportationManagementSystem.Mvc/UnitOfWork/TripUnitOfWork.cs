
using TransportationManagementSystem.Mvc.Data;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Repositories;
using TransportationManagementSystem.Mvc.Repositories.Interfaces;

namespace TransportationManagementSystem.Mvc.UnitOfWork
{
    public class TripUnitOfWork : ITripUnitOfWork
    {
        private readonly TripContext _context;
        private readonly IBulkOperationsProvider _bulkOps;

        private readonly Dictionary<Type, object> _repositories = new();

        public TripUnitOfWork(TripContext context, IBulkOperationsProvider bulkOps = null)
        {
            _context = context;
            _bulkOps = bulkOps ?? new EfCoreBulkOperationsProvider();
        }

        private Repository<T> GetRepository<T>() where T : class
        {
            if (!_repositories.TryGetValue(typeof(T), out var repository))
            {
                repository = new Repository<T>(_context, _bulkOps);
                _repositories.Add(typeof(T), repository);
            }

            return (Repository<T>)repository;
        }

        public Repository<Trip> Trips => GetRepository<Trip>();

        public Repository<Driver> Drivers => GetRepository<Driver>();

        public Repository<TripDate> TripDates => GetRepository<TripDate>();

        public Repository<Summary> Summaries => GetRepository<Summary>();

        public void Save() => _context.SaveChanges();

        public async Task SaveAsync(CancellationToken ct = default)
            => await _context.SaveChangesAsync(ct);
    }
}
