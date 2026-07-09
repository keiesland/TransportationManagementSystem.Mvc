using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text;
using TransportationManagementSystem.Mvc.Controllers;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services.Interfaces;

namespace TransportationManagementSystem.Mvc.Tests.Controllers
{
    public class FileControllerTests
    {
        private readonly Mock<IFileImportService> _mockFileImportService;
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _mockFileImportService = new Mock<IFileImportService>();
            _controller = new FileController(_mockFileImportService.Object);

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
        public async Task Import_ValidExcelFile_ReturnsSuccessMessage()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            var fileContent = "Fake Excel Data Content";
            var bytes = Encoding.UTF8.GetBytes(fileContent);

            mockFile.Setup(f => f.Length).Returns(bytes.Length);
            mockFile.Setup(f => f.FileName).Returns("trips.xlsx");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    stream.Write(bytes, 0, bytes.Length);
                })
                .Returns(Task.CompletedTask);

            var fileCollection = new FormFileCollection { mockFile.Object };
            _controller.Request.Form = new FormCollection(null, fileCollection);

            var mockResult = new ImportResult
            {
                IsValid = true,
                ImportedCount = 15
            };

            _mockFileImportService
                .Setup(s => s.ImportTripsAsync(It.IsAny<Stream>(), ".xlsx", CancellationToken.None))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.Import(CancellationToken.None);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("File Upload Complete! 15 trips imported.", contentResult.Content);
        }

        [Fact]
        public async Task Import_ValidationFails_ReturnsPartialViewWithErrors()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();

            var fileContent = "Fake Excel Data Content";
            var bytes = Encoding.UTF8.GetBytes(fileContent);

            mockFile.Setup(f => f.Length).Returns(bytes.Length);
            mockFile.Setup(f => f.FileName).Returns("trips.xlsx");
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, CancellationToken>((stream, token) =>
                {
                    stream.Write(bytes, 0, bytes.Length);
                })
                .Returns(Task.CompletedTask);

            var fileCollection = new FormFileCollection { mockFile.Object };
            _controller.Request.Form = new FormCollection(null, fileCollection);

            var mockErrors = new List<ValidationError>
            {
                new ValidationError
                {
                    Driver = "WILSON, TOMIKA",
                    TripDate = new DateTime(2024, 9, 23),
                    Message = "Clock out occurs at or before the first actual pickup."
                }
            };

            var mockResult = new ImportResult
            {
                IsValid = false,
                Errors = mockErrors
            };

            _mockFileImportService
                .Setup(s => s.ImportTripsAsync(It.IsAny<Stream>(), ".xlsx", CancellationToken.None))
                .ReturnsAsync(mockResult);

            // Act
            var result = await _controller.Import(CancellationToken.None);

            // Assert
            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ValidationErrorsPartial", partialViewResult.ViewName);

            var model = Assert.IsType<List<ValidationError>>(partialViewResult.Model);
            Assert.Single(model);
            Assert.Equal("WILSON, TOMIKA", model[0].Driver);
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
        }
    }
}