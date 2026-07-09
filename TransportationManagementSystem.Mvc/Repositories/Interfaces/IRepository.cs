using TransportationManagementSystem.Mvc.Data.Query;

namespace TransportationManagementSystem.Mvc.Repositories.Interfaces
{

    /// <summary>
    /// Generic repository contract for a single entity type.
    /// Existing members reflect the querying pattern already in use
    /// (QueryOptions-based filtering/sorting/paging). Bulk members are
    /// new additions that funnel EFCore.BulkExtensions calls through
    /// the same abstraction instead of bypassing it.
    /// </summary>
    public interface IRepository<T> where T : class
    {
        // ---------- Existing members (already in use) ----------

        int Count();
        int Count(QueryOptions<T> options);


        T Get(QueryOptions<T> options);
        Task<T> GetAsync(QueryOptions<T> options, CancellationToken ct = default);

        List<T> List(QueryOptions<T> options);
        Task<List<T>> ListAsync(QueryOptions<T> options, CancellationToken ct = default);

        void Insert(T entity);
        void Update(T entity);
        void Delete(T entity);

        // ---------- New: bulk operations ----------

        /// <summary>
        /// Bulk inserts a list of entities directly via SqlBulkCopy, bypassing
        /// EF's per-entity change tracking. Use for large imports.
        /// </summary>
        Task BulkInsertAsync(IEnumerable<T> entities, CancellationToken ct = default);

        /// <summary>
        /// Bulk deletes a list of entities directly, bypassing per-entity tracking.
        /// </summary>
        Task BulkDeleteAsync(IEnumerable<T> entities, CancellationToken ct = default);

        /// <summary>
        /// Bulk deletes every row currently in the table for this entity type.
        /// </summary>
        Task BulkDeleteAllAsync(CancellationToken ct = default);

        /// <summary>
        /// True if the table for this entity type currently has any rows.
        /// </summary>
        Task<bool> AnyAsync(CancellationToken ct = default);
    }


}

