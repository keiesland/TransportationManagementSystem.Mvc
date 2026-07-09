using TransportationManagementSystem.Data;
using TransportationManagementSystem.Models;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TransportationManagementSystem.Tests.Integration.Controllers
{
    public class TripControllerIntegrationTests : IClassFixture<TripControllerWebApplicationFactory>
    {
        private readonly TripControllerWebApplicationFactory _factory;

        public TripControllerIntegrationTests(TripControllerWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task List_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Trip/List");

            // Assert
            response.EnsureSuccessStatusCode(); // throws if not 2xx
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task List_WithSeededDriver_RendersDriverNameInHtml()
        {
            // Arrange: seed one driver, one trip date, and one trip so the
            // List view actually has something to render
            await _factory.SeedDataAsync(context =>
            {
                var driver = new Driver
                {
                    DriverId = 1,
                    FirstName = "Samuel",
                    LastName = "Adams",
                    FullName = "Samuel Adams"
                };

                var tripDate = new TripDate
                {
                    TripDateId = 1,
                    Date = new DateTime(2026, 6, 1),
                    WeekNumber = 22
                };

                var trip = new Trip
                {
                    TripId = 1,
                    DriverId = 1,
                    TripDateId = 1,
                    TripActualStart = TimeSpan.FromHours(8),
                    ScheduledPickup = TimeSpan.FromHours(8.5),
                    PickupArrival = TimeSpan.FromHours(8.4),
                    ActualPickup = TimeSpan.FromHours(8.5),
                    ActualDropoff = TimeSpan.FromHours(9),
                    ScheduledDropoff = TimeSpan.FromHours(9),
                    TripActualEnd = TimeSpan.FromHours(17)
                };

                context.Drivers.Add(driver);
                context.TripDates.Add(tripDate);
                context.Trips.Add(trip);
            });

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Trip/List");
            var html = await response.Content.ReadAsStringAsync();

            // Assert: the actual rendered HTML contains the driver's name —
            // this proves controller -> service -> repository -> view all
            // wired together correctly, end to end.
            response.EnsureSuccessStatusCode();
            Assert.Contains("Samuel Adams", html);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Trip/Details/99999");

            // Assert: TripController.Details now returns NotFound() when the
            // trip doesn't exist, instead of passing a null model to the view
            // (which previously threw a NullReferenceException inside Razor
            // — a real bug this test caught).
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}


