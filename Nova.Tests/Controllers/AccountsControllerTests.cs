using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nova.Tests.TestHelpers;
using Nova.Web.Controllers;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using Xunit;

namespace Nova.Tests.Controllers
{
    public class AccountsControllerTests
    {
        private readonly Mock<IUserServices> _serviceMock = new();
        private readonly Mock<IUtilityServices> _utilityMock = new();
        private readonly Mock<IUserActivities> _userActivitiesMock = new();
        private readonly Mock<IuserRoleService> _userRoleServiceMock = new();

        private AccountsController CreateSut()
        {
            var controller = new AccountsController(
                DbContextFactory.CreateContext(),
                _serviceMock.Object,
                _utilityMock.Object,
                _userActivitiesMock.Object,
                _userRoleServiceMock.Object,
                NullLogger<AccountsController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/Accounts/Login");
            controller.Url = urlHelperMock.Object;

            return controller;
        }

        private static object GetValue(JsonResult result, string propertyName)
        {
            var property = result.Value!.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            return property!.GetValue(result.Value)!;
        }

        [Fact]
        public async Task Login_Post_TriggersTwoFactorFlow_WhenCredentialsAreValid()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.CheckLogin(It.IsAny<UserViewModel>())).ReturnsAsync(new UserViewModel { Id = 5 });
            _serviceMock.Setup(x => x.Generate2FACode(5)).ReturnsAsync(true);
            _utilityMock.Setup(x => x.GetIPAddress()).ReturnsAsync("127.0.0.1");

            var result = await sut.Login(new UserViewModel { Username = "jdoe", Password = "correct" });

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(true, GetValue(json, "result"));
            Assert.Equal(5, GetValue(json, "id"));
            _serviceMock.Verify(x => x.Generate2FACode(5), Times.Once);
            _userActivitiesMock.Verify(x => x.SaveActivity(5, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Login_Post_RemembersUsername_WhenRememberMeChecked()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.CheckLogin(It.IsAny<UserViewModel>())).ReturnsAsync(new UserViewModel { Id = 5 });
            _utilityMock.Setup(x => x.GetIPAddress()).ReturnsAsync("127.0.0.1");

            await sut.Login(new UserViewModel { Username = "jdoe", Password = "correct", RememberMe = true });

            _utilityMock.Verify(x => x.SetCookies("NovaLogin", "jdoe", 30), Times.Once);
        }

        [Fact]
        public async Task Login_Post_ReturnsLoginUrl_WhenCredentialsAreInvalid()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.CheckLogin(It.IsAny<UserViewModel>())).ReturnsAsync(new UserViewModel());

            var result = await sut.Login(new UserViewModel { Username = "jdoe", Password = "wrong" });

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("/Accounts/Login", GetValue(json, "url"));
            _serviceMock.Verify(x => x.Generate2FACode(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Login_Post_ReturnsLoginUrl_WhenModelStateIsInvalid()
        {
            var sut = CreateSut();
            sut.ModelState.AddModelError("Username", "Username is required");

            var result = await sut.Login(new UserViewModel());

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("/Accounts/Login", GetValue(json, "url"));
            _serviceMock.Verify(x => x.CheckLogin(It.IsAny<UserViewModel>()), Times.Never);
        }

        [Fact]
        public async Task TwoFactorAuthentication_ReturnsUserListUrl_WhenCodeIsValid()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.Check2FACode(5, "123456")).ReturnsAsync(true);
            _utilityMock.Setup(x => x.GetIPAddress()).ReturnsAsync("127.0.0.1");

            var result = await sut.TwoFactorAuthentication(new UserViewModel { Id = 5, TwoFactorCode = "123456" });

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("/Accounts/Login", GetValue(json, "url")); // mocked helper always returns this
            _userActivitiesMock.Verify(x => x.SaveActivity(5, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task TwoFactorAuthentication_ReturnsLoginUrl_WhenCodeIsInvalid()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.Check2FACode(5, "000000")).ReturnsAsync(false);

            var result = await sut.TwoFactorAuthentication(new UserViewModel { Id = 5, TwoFactorCode = "000000" });

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("/Accounts/Login", GetValue(json, "url"));
            _userActivitiesMock.Verify(x => x.SaveActivity(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LogOut_SavesActivityAndLogsOut()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 3 });
            _utilityMock.Setup(x => x.GetIPAddress()).ReturnsAsync("127.0.0.1");

            var result = await sut.LogOut();

            _userActivitiesMock.Verify(x => x.SaveActivity(3, It.IsAny<string>()), Times.Once);
            _serviceMock.Verify(x => x.LogOut(), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Accounts", redirect.ControllerName);
        }

        [Fact]
        public async Task DeleteUser_ReturnsSuccess_WhenDeletionSucceeds()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.DeleteUserByUserID(10)).ReturnsAsync(true);
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1 });

            var result = await sut.DeleteUser(10);

            Assert.Equal(true, GetValue(result, "Result"));
        }

        [Fact]
        public async Task DeleteUser_ReturnsFailure_WhenDeletionFails()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.DeleteUserByUserID(10)).ReturnsAsync(false);

            var result = await sut.DeleteUser(10);

            Assert.Equal(false, GetValue(result, "Result"));
        }
    }
}
