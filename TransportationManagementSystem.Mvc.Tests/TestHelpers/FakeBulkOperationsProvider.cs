using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TransportationManagementSystem.Mvc.Repositories.Interfaces;

namespace TransportationManagementSystem.Mvc.TestHelpers
{
    public class FakeBulkOperationsProvider : IBulkOperationsProvider
    {
        // InMemory provider doesn't support real bulk operations, so this
        // fake just performs the equivalent Add/Remove + SaveChanges, which
        // InMemory handles fine and produces the same observable end state.
        public async Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            context.Set<T>().AddRange(entities);
            await context.SaveChangesAsync(ct);
        }

        public async Task BulkDeleteAsync<T>(DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
        {
            context.Set<T>().RemoveRange(entities);
            await context.SaveChangesAsync(ct);
        }
    }
}
