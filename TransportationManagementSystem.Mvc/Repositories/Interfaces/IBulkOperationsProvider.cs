using Microsoft.EntityFrameworkCore;

namespace TransportationManagementSystem.Mvc.Repositories.Interfaces
{
    /// <summary>
    /// Wraps EFCore.BulkExtensions calls behind an interface so Repository&lt;T&gt;
    /// doesn't call the third-party library directly. This means:
    ///   - Repository&lt;T&gt; can be tested with a MOCK of this interface, instead
    ///     of needing a real database connection that supports bulk operations.
    ///   - If EFCore.BulkExtensions has a bug/limitation against a specific
    ///     provider (as we found with SQLite), it's isolated to ONE small class
    ///     instead of leaking into every repository test.
    ///   - If you ever swap bulk-operation libraries, only this class changes.
    /// </summary>
    public interface IBulkOperationsProvider
    {
        Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class;
        Task BulkDeleteAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class;
    }
}
