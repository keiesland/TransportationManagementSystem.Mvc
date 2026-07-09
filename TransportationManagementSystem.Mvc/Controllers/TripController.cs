using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Services.Interfaces;
using TransportationManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace TransportationManagementSystem.Controllers
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
