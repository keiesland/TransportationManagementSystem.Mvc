using TransportationManagementSystem.Data;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Repositories.Interfaces;
using TransportationManagementSystem.Services;
using TransportationManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace TransportationManagementSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Only register the real SQL Server connection outside of tests.
            // The WebApplicationFactory-based integration tests set the
            // environment to "Testing" and register their own InMemory
            // TripContext instead — this avoids ever having TWO EF Core
            // providers (SqlServer + InMemory) registered in the same
            // service collection at once, which EF Core does not allow.
            builder.Services.AddDbContext<TripContext>(options =>
                 options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
  

            // For example, if we want a new instance of the repository for each HTTP request, we can use AddScoped.
            // If we want a single instance of the repository for the entire application, we can use AddSingleton.
            builder.Services.AddScoped<ITripService, TripService>();
            builder.Services.AddScoped<ISummaryService, SummaryService>();
            builder.Services.AddScoped<IFileImportService, FileImportService>();
            builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Skip the production-style exception handler under "Testing" too,
            // so integration tests see the REAL exception (via TestServer
            // rethrowing it) instead of a generic error page with no detail.
            if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.UseSession();

            app.MapStaticAssets();

            app.MapControllerRoute(
                    name: "",
                    pattern:
                    "{controller}/{action}/page/{pagenumber}/size/{pagesize}/sort/{sortfield}/{sortdirection}/filter/{driver}/{tripdate}")
                .WithStaticAssets();

            app.MapControllerRoute(
                    name: "",
                    pattern: "{controller}/{action}/page/{pagenumber}/size/{pagesize}/sort/{sortfield}/{sortdirection}")
                .WithStaticAssets();

            app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
