using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Services
{
    public class ValidationService : IValidationService
    {
        public ValidationResult Validate(List<DriverDay> driverDays)
        {
            var errors = new List<ValidationError>();

            foreach (var day in driverDays)
            {
                var workingTrips = day.WorkingTrips;

                if (workingTrips.Count == 0)
                {
                    errors.Add(new ValidationError
                    {
                        Driver = day.Driver,
                        TripDate = day.TripDate,
                        Message = "All trips for this driver day are marked as no-shows — this is unusual and needs review."
                    });
                    continue;
                }

                var firstTrip = workingTrips.First(); // sorted chronologically by Aggregation

                if (day.TripActualEnd <= firstTrip.ActualPickupTime)
                {
                    errors.Add(new ValidationError
                    {
                        Driver = day.Driver,
                        TripDate = day.TripDate,
                        Message = $"Clock out ({day.TripActualEnd}) occurs at or before the first actual pickup ({firstTrip.ActualPickupTime}) — clock out time appears invalid."
                    });
                }

                if (day.TripActualEnd <= day.TripActualStart)
                {
                    errors.Add(new ValidationError
                    {
                        Driver = day.Driver,
                        TripDate = day.TripDate,
                        Message = $"Clock out ({day.TripActualEnd}) is before or equal to clock in ({day.TripActualStart})."
                    });
                }

                foreach (var trip in workingTrips)
                {
                    if (trip.ActualDropoffTime <= trip.ActualPickupTime)
                    {
                        errors.Add(new ValidationError
                        {
                            Driver = day.Driver,
                            TripDate = day.TripDate,
                            Message = $"Dropoff ({trip.ActualDropoffTime}) is before or equal to pickup ({trip.ActualPickupTime})."
                        });
                    }
                }
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }
    }
}
