using Microsoft.AspNetCore.Mvc;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Controllers
{
    public class TripController : Controller
    {
        private readonly ITripService _tripService;

        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }

        public RedirectToActionResult Index() => RedirectToAction("List");

        public ViewResult List(TripGridDTO values)
        {
            var vm = _tripService.GetTripsForList(values, HttpContext.Session);
            return View(vm);
        }

        public IActionResult Details(int id)
        {
            var trip = _tripService.GetTripDetails(id);

            if (trip == null)
            {
                return NotFound();
            }

            return View(trip);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public RedirectToActionResult Filter(string[] filter, bool clear = false)
        {
            var routes = _tripService.ApplyFilter(filter, clear, HttpContext.Session);
            return RedirectToAction("List", routes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public RedirectToActionResult PageSize(int pagesize)
        {
            _tripService.UpdatePageSize(pagesize, HttpContext.Session);
            return RedirectToAction("List");
        }
    }
}
