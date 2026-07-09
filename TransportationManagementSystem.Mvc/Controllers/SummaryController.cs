using ClosedXML.Excel;
using TransportationManagementSystem.UtilityClasses;
using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.DTOs;
using TransportationManagementSystem.Data.Grid;
using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Services.Interfaces;
using TransportationManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace TransportationManagementSystem.Controllers
{
    public class SummaryController : Controller
    {
        private readonly ISummaryService _summaryService;

        public SummaryController(ISummaryService summaryService)
        {
            _summaryService = summaryService;
        }

        public async Task<RedirectToActionResult> Index(CancellationToken ct)
        {
            await _summaryService.SummarizeAndResetAsync(ct);
            return RedirectToAction("List");
        }

        public async Task<ViewResult> List(SummaryGridDTO values, CancellationToken ct)
        {
            var vm = await _summaryService.GetSummariesForListAsync(values, HttpContext.Session, ct);
            return View(vm);
        }

        public async Task<ViewResult> Details(int id, CancellationToken ct)
        {
            var summary = await _summaryService.GetSummaryDetailsAsync(id, ct);
            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<RedirectToActionResult> Filter(string[] filter, bool clear = false, CancellationToken ct = default)
        {
            var routes = await _summaryService.ApplyFilterAsync(filter, clear, HttpContext.Session);
            return RedirectToAction("List", routes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<RedirectToActionResult> PageSize(int pagesize, CancellationToken ct = default)
        {
            await _summaryService.UpdatePageSizeAsync(pagesize, HttpContext.Session);
            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportAndClear(CancellationToken ct)
        {
            var (fileBytes, filename) = await _summaryService.ExportAndClearAsync(ct);

            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
    }

}
