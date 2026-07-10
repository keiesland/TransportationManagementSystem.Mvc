using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TransportationManagementSystem.Mvc.Controllers;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.ViewModels;

namespace TransportationManagementSystem.Mvc.Tests.Controllers
{
    public class SummaryControllerTests
    {
        private readonly Mock<ISummaryService> _mockSummaryService;
        private readonly Mock<ISession> _mockSession;
        private readonly SummaryController _controller;

        public SummaryControllerTests()
        {
            _mockSummaryService = new Mock<ISummaryService>();
            _mockSession = new Mock<ISession>();

            // Initialize the controller once using the class fields
            _controller = new SummaryController(_mockSummaryService.Object);

            // Globally attach the mocked Session and HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Session = _mockSession.Object;
        }
        
        [Fact]
        public async Task List_ReturnsViewResult_WithCorrectViewModelAsync()
        {
            var dtoValues = new SummaryGridDTO();
            var expectedVm = new SummaryListViewModel( );

            _mockSummaryService
                .Setup(s => s.GetSummariesForListAsync(dtoValues, _mockSession.Object, CancellationToken.None))
                .ReturnsAsync(expectedVm);
            
            var result = await _controller.List(dtoValues, CancellationToken.None);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<SummaryListViewModel>(viewResult.ViewData.Model);

            Assert.Same(expectedVm, model);
        }

        [Fact]
        public async Task Filter_InvokesApplyFilterAsync_AndRedirectsToList()
        {
            string[] testFilters = new[] { "destination", "price" };
            bool testClear = true;

            var result = await _controller.Filter(testFilters, testClear);

            _mockSummaryService.Verify(s => s.ApplyFilterAsync(
                testFilters,
                testClear,
                _mockSession.Object
            ), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); 
        }

        [Fact]
        public async Task PageSize_InvokesUpdatePageSizeAsync_AndRedirectsToList()
        {
            int pageSize = 10;

            var result = await _controller.PageSize(pageSize);

            _mockSummaryService.Verify(s => s.UpdatePageSizeAsync(
                pageSize,
                _mockSession.Object
            ), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); // Null because it redirects within the same controller
        }

        [Fact]
        public async Task ExportAndClear_ReturnsFileResult_WithCorrectData()
        {
            byte[] expectedBytes = new byte[] { 1, 2, 3, 4 };
            string expectedFilename = "trips_summary.xlsx";
            var serviceResult = (fileBytes: expectedBytes, filename: expectedFilename);

            // Set up the mock service to return the tuple when called
            _mockSummaryService
                .Setup(s => s.ExportAndClearAsync(CancellationToken.None))
                .ReturnsAsync(serviceResult);

            // 2. Act
            var result = await _controller.ExportAndClear(CancellationToken.None);

            // 3. Assert - Part A: Verify Service Execution
            _mockSummaryService.Verify(s => s.ExportAndClearAsync(CancellationToken.None), Times.Once);

            // 3. Assert - Part B: Verify File Content Result
            var fileResult = Assert.IsType<FileContentResult>(result);


            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
            Assert.Equal(expectedFilename, fileResult.FileDownloadName);
            Assert.Equal(expectedBytes, fileResult.FileContents);
        }

        [Fact]
        public async Task Index_InvokesSummarizeAndReset_AndRedirectsToList()
        {
            var ct = CancellationToken.None;
            var result = await _controller.Index(ct);

            _mockSummaryService.Verify(s => s.SummarizeAndResetAsync(ct), Times.Once);

            // 3. Assert - Part B: Verify the Redirect
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName);
        }
    }
}
