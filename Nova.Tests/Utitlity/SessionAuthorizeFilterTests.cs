using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Nova.Web.Interfaces;
using Nova.Web.ViewModels;
using Xunit;

namespace Nova.Tests.Utitlity
{
    public class SessionAuthorizeFilterTests
    {
        private readonly Mock<IUserServices> _userServicesMock = new();

        private static AuthorizationFilterContext CreateContext()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        [Fact]
        public void OnAuthorization_RedirectsToLogin_WhenNoUserInSession()
        {
            _userServicesMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 0 });
            var sut = new SessionAuthorizeFilter("", _userServicesMock.Object);
            var context = CreateContext();

            sut.OnAuthorization(context);

            var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Accounts", redirect.ControllerName);
        }

        [Fact]
        public void OnAuthorization_AllowsRequest_WhenUserLoggedInAndNoRoleRequired()
        {
            _userServicesMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1, Rolename = "Admin" });
            var sut = new SessionAuthorizeFilter("", _userServicesMock.Object);
            var context = CreateContext();

            sut.OnAuthorization(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void OnAuthorization_AllowsRequest_WhenUserHasRequiredRole()
        {
            _userServicesMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1, Rolename = "Admin" });
            var sut = new SessionAuthorizeFilter("Admin", _userServicesMock.Object);
            var context = CreateContext();

            sut.OnAuthorization(context);

            Assert.Null(context.Result);
        }

        [Fact]
        public void OnAuthorization_RedirectsToLogin_WhenUserLacksRequiredRole()
        {
            _userServicesMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1, Rolename = "Viewer" });
            var sut = new SessionAuthorizeFilter("Admin", _userServicesMock.Object);
            var context = CreateContext();

            sut.OnAuthorization(context);

            var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Login", redirect.ActionName);
        }
    }
}
