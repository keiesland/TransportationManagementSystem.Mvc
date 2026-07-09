using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportationManagementSystem.Mvc.Data;

namespace TransportationManagementSystem.Mvc.Tests.Integration.Controllers
{
    /// <summary>
    /// Customizes the real app's startup for testing: swaps the real SQL
    /// Server TripContext registration for an InMemory one, so tests don't
    /// need a real database connection at all.
    /// </summary>
    public class TripControllerWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Environment-based detection didn't work reliably for this
                // app's minimal-hosting Program.cs (WebApplicationFactory
                // bootstraps via the real entry point before our environment
                // overrides take effect). Instead, brute-force remove EVERY
                // EF-Core-namespaced service descriptor that the real
                // Program.cs's AddDbContext<TripContext>(UseSqlServer) call
                // registered — not just DbContextOptions<TripContext> — since
                // UseSqlServer() also registers SqlServer's internal provider
                // services into the same collection, and leaving those behind
                // is what caused the "multiple providers registered" error.
                var efCoreDescriptors = services
                    .Where(d => d.ServiceType.Namespace != null &&
                                d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore"))
                    .ToList();

                foreach (var descriptor in efCoreDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TripContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });
            });
        }

        /// <summary>
        /// Seeds the InMemory database with known test data, using a fresh
        /// scoped TripContext (same pattern as production DI would use).
        /// </summary>
        public async Task SeedDataAsync(Action<TripContext> seedAction)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TripContext>();

            seedAction(context);
            await context.SaveChangesAsync();
        }
    }
}