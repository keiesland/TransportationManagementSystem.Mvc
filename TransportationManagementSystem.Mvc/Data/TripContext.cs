using TransportationManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace TransportationManagementSystem.Data
{
    public class TripContext : DbContext
    {
        public TripContext(DbContextOptions<TripContext> options)
            : base(options)
        {

        }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripDate> TripDates { get; set; }
        public DbSet<Summary> Summaries { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Summary>()
                .HasIndex(s => new { s.DriverId, s.TripDateId, s.Start, s.Out1 });

            modelBuilder.Entity<Trip>()
                .HasIndex(t => new { t.DriverId, t.TripDateId, t.TripActualStart, t.ScheduledPickup });
        }


    }
}
