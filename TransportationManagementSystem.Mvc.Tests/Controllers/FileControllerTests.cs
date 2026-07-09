using TransportationManagementSystem.Controllers;
using TransportationManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace TransportationManagementSystem.Tests.Controllers
{
    public class FileControllerTests
    {
        private readonly Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment> _mockHostingEnvironment;
        private readonly Mock<IFileImportService> _mockFileImportService;
        private readonly FileController _controller;
        private readonly string _tempTestFolder;

        public FileControllerTests()
        {
            _mockFileImportService = new Mock<IFileImportService>();
            _mockHostingEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

            // Create a unique temporary folder just for this test run
            _tempTestFolder = Path.Combine(Path.GetTempPath(), "TripTests_" + Guid.NewGuid().ToString());

            _mockHostingEnvironment
                .Setup(m => m.WebRootPath)
                .Returns(_tempTestFolder);

            _controller = new FileController(_mockHostingEnvironment.Object, _mockFileImportService.Object);

            // Standard setup: build HttpContext wrapper
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public void File_Index_ReturnsView()
        {
            var result = _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.True(string.IsNullOrEmpty(viewResult.ViewName));
            Assert.Null(viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Import_ValidExcelFile_SavesToDiskAndReturnsSuccessMessage()
        {
            // 1. Arrange
            var mockFile = new Mock<IFormFile>();

            // Prepare dummy string content to act as the raw bytes of the file
            var fileContent = "Fake Excel Data Content";
            var bytes = Encoding.UTF8.GetBytes(fileContent);
            var memoryStream = new MemoryStream(bytes);

            // Configure the mocked file's properties and CopyToAsync method
            mockFile.Setup(f => f.Length).Returns(bytes.Length);
            mockFile.Setup(f => f.FileName).Returns("trips.xlsx");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    // Write our dummy file data into whatever stream the controller opens
                    stream.Write(bytes, 0, bytes.Length);
                })
                .Returns(Task.CompletedTask);

            // Put the mocked file into the HTTP request's Form File Collection
            var fileCollection = new FormFileCollection { mockFile.Object };
            _controller.Request.Form = new FormCollection(null, fileCollection);

            // Setup service call response
            int mockImportedCount = 15;
            _mockFileImportService
                .Setup(s => s.ImportTripsAsync(It.IsAny<Stream>(), ".xlsx", CancellationToken.None))
                .ReturnsAsync(mockImportedCount);

            // 2. Act
            var result = await _controller.Import(CancellationToken.None);

            // 3. Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("File Upload Complete! 15 trips imported.", contentResult.Content);

            // Verify the file actually successfully created the directory and file on disk
            var expectedSavedFilePath = Path.Combine(_tempTestFolder, "UploadExcel", "trips.xlsx");
            Assert.True(File.Exists(expectedSavedFilePath));
        }

        [Fact]
        public async Task Import_FileLengthIsZero_ReturnsWarningMessageAndDoesNotImport()
        {
            var mockFile = new Mock<IFormFile>();

            mockFile.Setup(f => f.Length).Returns(0);
            mockFile.Setup(f => f.FileName).Returns("empty_file.xlsx");

            var fileCollection = new FormFileCollection { mockFile.Object };
            _controller.Request.Form = new FormCollection(null, fileCollection);

            var result = await _controller.Import(CancellationToken.None);

            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("No file was uploaded.", contentResult.Content);

            _mockFileImportService.Verify(
                s => s.ImportTripsAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never
            );

            var uploadDirectoryPath = Path.Combine(_tempTestFolder, "UploadExcel");
            Assert.False(Directory.Exists(uploadDirectoryPath));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempTestFolder))
            {
                Directory.Delete(_tempTestFolder, recursive: true);
            }
        }
    }
}

