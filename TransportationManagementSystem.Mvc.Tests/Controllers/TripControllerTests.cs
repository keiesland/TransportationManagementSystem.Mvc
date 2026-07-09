using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TransportationManagementSystem.Mvc.Controllers;
using TransportationManagementSystem.Mvc.Data.DTOs;
using TransportationManagementSystem.Mvc.Entities;
using TransportationManagementSystem.Mvc.Services.Interfaces;
using TransportationManagementSystem.Mvc.ViewModels;

namespace TransportationManagementSystem.Mvc.Tests.Controllers
{
    public class TripControllerTests
    {
        private readonly Mock<ITripService> _mockTripService;
        private readonly Mock<ISession> _mockSession;
        private readonly TripController _controller;

        public TripControllerTests()
        {
            _mockTripService = new Mock<ITripService>();
            _mockSession = new Mock<ISession>();

            // Initialize the controller once using the class fields
            _controller = new TripController(_mockTripService.Object);

            // Globally attach the mocked Session and HttpContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.HttpContext.Session = _mockSession.Object;
        }

        [Fact]
        public void Trip_Index_RedirectsToTripList()
        {

            var result = _controller.Index();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName);
        }

        [Fact]
        public void List_ReturnsViewResult_WithCorrectViewModel()
        {
            var dtoValues = new TripGridDTO();
            var expectedVm = new TripListViewModel();

            _mockTripService
                .Setup(s => s.GetTripsForList(dtoValues, _mockSession.Object))
                .Returns(expectedVm);

            var result = _controller.List(dtoValues);

            // 3. Assert
            // Verify the action returns a ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);

            var model = Assert.IsAssignableFrom<TripListViewModel>(viewResult.ViewData.Model);
            Assert.Same(expectedVm, model); 
        }

        [Fact]
        public void Details_ReturnsViewResult_WithCorrectView()
        {
            var id = 1;
            var trip = new Trip();
          
            _mockTripService
                .Setup(s => s.GetTripDetails(id))
                .Returns(trip);
      
            var result = _controller.Details(id);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Trip>(viewResult.ViewData.Model);
            Assert.Same(trip, model); 
        }

        [Fact]
        public void Filter_InvokesApplyFilter_AndRedirectsToList()
        {
            string[] testFilters = new[] { "destination", "price" };
            bool testClear = true;

            var result = _controller.Filter(testFilters, testClear);

            _mockTripService.Verify(s => s.ApplyFilter(
                testFilters,
                testClear,
                _mockSession.Object
            ), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName);
        }

        [Fact]
        public void PageSize_InvokesUpdatePageSize_AndRedirectsToList()
        {
            int pageSize = 10;
            
            var result = _controller.PageSize(pageSize);

            _mockTripService.Verify(s => s.UpdatePageSize(
                    pageSize,
                    _mockSession.Object
                    ), Times.Once);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("List", redirectResult.ActionName);
            Assert.Null(redirectResult.ControllerName); 
        }
    }
}
