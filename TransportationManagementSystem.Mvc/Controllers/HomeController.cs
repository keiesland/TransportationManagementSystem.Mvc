using TransportationManagementSystem.UtilityClasses;
using TransportationManagementSystem.Data;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Services.Interfaces;
using TransportationManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace TransportationManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITripService _tripService;

        public HomeController(ITripService tripService)
        {
            _tripService = tripService;
        }

        public ViewResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Clear(CancellationToken ct)
        {
            await _tripService.ClearAllDataAsync(ct);
            return View("Index");
        }
    }
}
