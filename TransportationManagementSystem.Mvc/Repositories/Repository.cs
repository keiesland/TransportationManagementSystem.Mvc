using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Repositories.Interfaces;
using TransportationManagementSystem.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace TransportationManagementSystem.Repositories
{
    /// <summary>
    /// Generic repository implementation. ApplyOptions() matches the confirmed
    /// QueryOptions&lt;T&gt; shape: GetIncludes() returns a string[] of include paths,
    /// WhereClauses is a List&lt;Expression&lt;Func&lt;T,bool&gt;&gt;&gt; applied as successive
    /// .Where() calls, and HasOrderBy/HasPaging gate optional sorting/paging.
    /// </summary>
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbContext _context;
        private readonly DbSet<T> _dbSet;
        private readonly IBulkOperationsProvider _bulkOps;

        public Repository(DbContext context, IBulkOperationsProvider bulkOps = null)
        {
            _context = context;
            _dbSet = context.Set<T>();
            // Defaulting to the real implementation keeps existing call sites
            // (`new Repository<Driver>(context)`) working unchanged in production
            // code, while tests can pass a mock explicitly.
            _bulkOps = bulkOps ?? new EfCoreBulkOperationsProvider();
        }

        public int Count() => _dbSet.Count();

        public int Count(QueryOptions<T> options)
        {
            return ApplyFilterOnly(options).Count();
        }

        public T Get(QueryOptions<T> options)
        {
            return ApplyOptions(options).FirstOrDefault();
        }

        public async Task<T> GetAsync(QueryOptions<T> options, CancellationToken ct = default)
        {
            return await ApplyOptions(options).FirstOrDefaultAsync(ct);
        }

        public List<T> List(QueryOptions<T> options)
        {
            return ApplyOptions(options).ToList();
        }

        public async Task<List<T>> ListAsync(QueryOptions<T> options, CancellationToken ct = default)
        {
            return await ApplyOptions(options).ToListAsync(ct);
        }

        public void Insert(T entity) => _dbSet.Add(entity);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        // ---------- Bulk operations ----------

        public async Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            await _bulkOps.BulkInsertAsync(_context, entities, ct);
        }

        public async Task BulkDeleteAsync(IEnumerable<T> entities, CancellationToken ct = default)
        {
            await _bulkOps.BulkDeleteAsync(_context, entities, ct);
        }

        public async Task BulkDeleteAllAsync(CancellationToken ct = default)
        {
            if (!await _dbSet.AnyAsync(ct))
            {
                return;
            }

            var all = await _dbSet.ToListAsync(ct);
            await _bulkOps.BulkDeleteAsync(_context, all, ct);
        }

        public async Task<bool> AnyAsync(CancellationToken ct = default)
        {
            return await _dbSet.AnyAsync(ct);
        }

        // ---------- Helpers ----------

        private IQueryable<T> ApplyOptions(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in options.GetIncludes())
            {
                if (!string.IsNullOrWhiteSpace(include))
                {
                    query = query.Include(include);
                }
            }

            if (options.HasWhere)
            {
                foreach (var whereClause in options.WhereClauses)
                {
                    query = query.Where(whereClause);
                }
            }

            if (options.HasOrderBy)
            {
                query = options.OrderByDirection == "desc"
                    ? query.OrderByDescending(options.OrderBy)
                    : query.OrderBy(options.OrderBy);
            }

            if (options.HasPaging)
            {
                query = query
                    .Skip((options.PageNumber - 1) * options.PageSize)
                    .Take(options.PageSize);
            }

            return query;
        }

        private IQueryable<T> ApplyFilterOnly(QueryOptions<T> options)
        {
            IQueryable<T> query = _dbSet;

            if (options.HasWhere)
            {
                foreach (var whereClause in options.WhereClauses)
                {
                    query = query.Where(whereClause);
                }
            }

            return query;
        }
    }

}
