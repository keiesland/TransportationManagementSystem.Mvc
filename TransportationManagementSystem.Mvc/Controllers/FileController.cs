using DocumentFormat.OpenXml.Office.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileImportService _fileImportService;

        public FileController(IFileImportService fileImportService)
        {
            _fileImportService = fileImportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Import(CancellationToken ct)
        {
            IFormFile file = Request.Form.Files[0];

            if (file.Length == 0)
            {
                return Content("No file was uploaded.");
            }

            string fileExtension = Path.GetExtension(file.FileName).ToLower();

            ImportResult result;
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, ct);
                stream.Position = 0;
                result = await _fileImportService.ImportTripsAsync(stream, fileExtension, ct);
            }

            if (!result.IsValid)
            {
                return PartialView("_ValidationErrorsPartial", result.Errors);
            }

            return Content($"File Upload Complete! {result.ImportedCount} trips imported.");
        }

        public IActionResult ValidationErrors()
        {
            var json = TempData["ValidationErrors"] as string;
            var errors = string.IsNullOrEmpty(json)
                ? new List<ValidationError>()
                : JsonSerializer.Deserialize<List<ValidationError>>(json);

            return View(errors);
        }
    }
}
