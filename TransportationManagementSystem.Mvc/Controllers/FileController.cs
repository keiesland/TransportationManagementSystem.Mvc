using Azure.Core;
using EFCore.BulkExtensions;
using TransportationManagementSystem.UtilityClasses;
using TransportationManagementSystem.Data;
using TransportationManagementSystem.Data.Query;
using TransportationManagementSystem.Models;
using TransportationManagementSystem.Repositories;
using TransportationManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TransportationManagementSystem.Controllers
{
    public class FileController : Controller
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IFileImportService _fileImportService;

        public FileController(IWebHostEnvironment hostingEnvironment, IFileImportService fileImportService)
        {
            _hostingEnvironment = hostingEnvironment;
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

            // Controller's job: handle file system / web hosting concerns
            string folderName = "UploadExcel";
            string webRootPath = _hostingEnvironment.WebRootPath;
            string newPath = Path.Combine(webRootPath, folderName);

            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            string fullPath = Path.Combine(newPath, file.FileName);

            int importedCount;

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
                stream.Position = 0;

                // Service's job: parse the file and import the data
                importedCount = await _fileImportService.ImportTripsAsync(stream, fileExtension, ct);
            }

            return Content($"File Upload Complete! {importedCount} trips imported.");
        }
    }

}
