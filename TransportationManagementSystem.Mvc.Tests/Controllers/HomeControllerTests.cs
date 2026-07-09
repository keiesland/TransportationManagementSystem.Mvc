using System.Drawing.Text;
using TransportationManagementSystem.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TransportationManagementSystem.Repositories.Interfaces;
using TransportationManagementSystem.Services.Interfaces;

namespace TransportationManagementSystem.Tests.Controllers
{
    public class HomeControllerTests
    {
        private readonly ITripService _tripService;
        private readonly CancellationToken _ct = CancellationToken.None;

        private HomeController CreateController()
        {
            return new HomeController(_tripService);
        }


        [Fact]
        public void Home_Index_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Clear_CallsService_AndReturnsIndexView()
        {
            var mockService = new Mock<ITripService>();

            var controller = new HomeController(
                mockService.Object);

            var result = await controller.Clear(
                CancellationToken.None);

            mockService.Verify(
                s => s.ClearAllDataAsync(
                    It.IsAny<CancellationToken>()),
                Times.Once);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("Index", viewResult.ViewName);
        }
    }
}
