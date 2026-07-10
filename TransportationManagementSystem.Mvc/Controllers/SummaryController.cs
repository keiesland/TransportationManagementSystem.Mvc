using Microsoft.AspNetCore.Mvc;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Controllers
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
