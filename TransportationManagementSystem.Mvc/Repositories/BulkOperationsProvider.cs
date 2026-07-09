using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using EFCore.BulkExtensions;
using TransportationManagementSystem.Repositories.Interfaces;

namespace TransportationManagementSystem.Tests.TestHelpers
{
    /// <summary>
    /// Real implementation — thin pass-through to EFCore.BulkExtensions.
    /// This class is intentionally NOT unit tested directly; its job is
    /// trivial (forward the call), and verifying that the underlying
    /// library's bulk SQL generation actually works correctly against a
    /// given provider is the library's own responsibility, not ours.
    /// If you want a true end-to-end check, that belongs in a manual or
    /// CI integration test against a real SQL Server/LocalDB instance,
    /// not the fast unit test suite.
    /// </summary>
    public class EfCoreBulkOperationsProvider : IBulkOperationsProvider
    {
        public async Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            await context.BulkInsertAsync(entities.ToList(), cancellationToken: ct);
        }

        public async Task BulkDeleteAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            await context.BulkDeleteAsync(entities.ToList(), cancellationToken: ct);
        }
    }

}
