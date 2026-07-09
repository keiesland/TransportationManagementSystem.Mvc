using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.DomainModels;

namespace TransportationManagementSystem.Mvc.Services.Interfaces
{
    public interface IValidationService
    {
        ValidationResult Validate(List<DriverDay> driverDays);
    }
}
