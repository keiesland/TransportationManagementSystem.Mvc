using Microsoft.AspNetCore.Mvc;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Controllers
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
