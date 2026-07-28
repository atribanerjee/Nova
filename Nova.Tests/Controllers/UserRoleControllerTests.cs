using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;
using Nova.Tests.TestHelpers;
using Nova.Web.Controllers;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using Xunit;

namespace Nova.Tests.Controllers
{
    public class UserRoleControllerTests
    {
        private readonly Mock<IUserServices> _serviceMock = new();
        private readonly Mock<IUtilityServices> _utilityMock = new();
        private readonly Mock<IuserRoleService> _userRoleMock = new();
        private DefaultHttpContext _httpContext = new();

        private UserRoleController CreateSut()
        {
            return new UserRoleController(
                DbContextFactory.CreateContext(),
                _serviceMock.Object,
                _utilityMock.Object,
                _userRoleMock.Object,
                NullLogger<UserRoleController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContext },
                TempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        private static object GetValue(object result, string propertyName)
        {
            var value = result switch
            {
                JsonResult json => json.Value,
                OkObjectResult ok => ok.Value,
                _ => throw new InvalidOperationException($"Unsupported result type {result.GetType()}")
            };
            var property = value!.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            return property!.GetValue(value)!;
        }

        // ---------- Index ----------

        [Fact]
        public void Index_ReturnsView()
        {
            var result = CreateSut().Index();

            Assert.IsType<ViewResult>(result);
        }

        // ---------- RoleLIst ----------

        [Fact]
        public async Task RoleLIst_ReturnsViewWithRoles_WhenUserIsLoggedIn()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1 });
            var roles = new List<UserRoleViewModel> { new() { Id = 1, Rolename = "Admin" } };
            _userRoleMock.Setup(x => x.GetAllRoleList(It.IsAny<UserRoleViewModel>(), "")).ReturnsAsync(roles);

            var result = await sut.RoleLIst();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(roles, view.Model);
        }

        [Fact]
        public async Task RoleLIst_ReturnsEmptyView_WhenUserIsNotLoggedIn()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 0 });

            var result = await sut.RoleLIst();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Null(view.Model);
            _userRoleMock.Verify(x => x.GetAllRoleList(It.IsAny<UserRoleViewModel>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RoleLIst_ReturnsEmptyView_WhenAnExceptionIsThrown()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Throws(new InvalidOperationException("boom"));

            var result = await sut.RoleLIst();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Null(view.Model);
        }

        // ---------- GetRoleLIst ----------

        [Fact]
        public async Task GetRoleLIst_ReturnsPagedJsonResult()
        {
            var sut = CreateSut();
            _httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["draw"] = "1",
                ["start"] = "0",
                ["length"] = "10",
                ["search[value]"] = "adm"
            });
            var roles = new List<UserRoleViewModel> { new() { Id = 1, Rolename = "Admin", TotalRecords = 1 } };
            _userRoleMock.Setup(x => x.GetAllRoleList(It.IsAny<UserRoleViewModel>(), It.IsAny<string>())).ReturnsAsync(roles);

            var result = await sut.GetRoleLIst("");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal("1", GetValue(json, "draw"));
            Assert.Equal(1, GetValue(json, "recordsTotal"));
            Assert.Same(roles, GetValue(json, "data"));
            _userRoleMock.Verify(x => x.GetAllRoleList(It.Is<UserRoleViewModel>(m => m.PageSize == 10 && m.PageNumber == 0), "adm"), Times.Once);
        }

        // ---------- Add (GET) ----------

        [Fact]
        public async Task Add_Get_ReturnsViewWithEmptyModel()
        {
            var result = await CreateSut().Add();

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<UserRoleViewModel>(view.Model);
        }

        // ---------- Add (POST) ----------

        [Fact]
        public async Task Add_Post_ReturnsSuccess_WhenRoleIsSaved()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1 });
            _userRoleMock.Setup(x => x.CheckDuplicateRoleName("Manager")).ReturnsAsync(false);
            _userRoleMock.Setup(x => x.AddNewRole(It.IsAny<UserRoleViewModel>())).ReturnsAsync(true);

            var result = await sut.Add("Manager");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(true, GetValue(ok, "Result"));
        }

        [Fact]
        public async Task Add_Post_ReturnsFailure_WhenSaveFails()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.CheckDuplicateRoleName("Manager")).ReturnsAsync(false);
            _userRoleMock.Setup(x => x.AddNewRole(It.IsAny<UserRoleViewModel>())).ReturnsAsync(false);

            var result = await sut.Add("Manager");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(false, GetValue(ok, "Result"));
        }

        [Fact]
        public async Task Add_Post_ReturnsFailure_WhenRoleNameIsDuplicate()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.CheckDuplicateRoleName("Manager")).ReturnsAsync(true);

            var result = await sut.Add("Manager");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(false, GetValue(ok, "Result"));
            Assert.Equal("Role already exists.", GetValue(ok, "Message"));
            _userRoleMock.Verify(x => x.AddNewRole(It.IsAny<UserRoleViewModel>()), Times.Never);
        }

        [Fact]
        public async Task Add_Post_ReturnsFailure_WhenRoleNameIsEmpty()
        {
            var result = await CreateSut().Add("");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(false, GetValue(json, "Result"));
            _userRoleMock.Verify(x => x.CheckDuplicateRoleName(It.IsAny<string>()), Times.Never);
        }

        // ---------- Edit (GET) ----------

        [Fact]
        public async Task Edit_Get_ReturnsViewWithRoleDetail()
        {
            var sut = CreateSut();
            var role = new UserRoleViewModel { Id = 5, Rolename = "Manager" };
            _userRoleMock.Setup(x => x.GetRoleDetailByID(5)).ReturnsAsync(role);

            var result = await sut.Edit(5);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(role, view.Model);
        }

        // ---------- Edit (POST) ----------

        [Fact]
        public async Task Edit_Post_ReturnsSuccess_WhenUpdated()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1 });
            _userRoleMock.Setup(x => x.UpdateRole(It.IsAny<UserRoleViewModel>())).ReturnsAsync(true);

            var result = await sut.Edit(5, "Senior Manager");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(true, GetValue(ok, "Result"));
        }

        [Fact]
        public async Task Edit_Post_ReturnsFailure_WhenUpdateFails()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.UpdateRole(It.IsAny<UserRoleViewModel>())).ReturnsAsync(false);

            var result = await sut.Edit(5, "Senior Manager");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(false, GetValue(ok, "Result"));
        }

        [Fact]
        public async Task Edit_Post_ReturnsFailure_WhenRoleNameIsEmpty()
        {
            var result = await CreateSut().Edit(5, "");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(false, GetValue(ok, "Result"));
            _userRoleMock.Verify(x => x.UpdateRole(It.IsAny<UserRoleViewModel>()), Times.Never);
        }

        // ---------- CheckDulicateExceptMe ----------

        [Fact]
        public async Task CheckDulicateExceptMe_ReturnsTrue_WhenDuplicateFound()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.CheckDuplicateRoleNameExceptMe(5, "Manager")).ReturnsAsync(true);

            var result = await sut.CheckDulicateExceptMe(5, "Manager");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(true, GetValue(json, "Result"));
        }

        [Fact]
        public async Task CheckDulicateExceptMe_ReturnsFalse_WhenNoDuplicate()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.CheckDuplicateRoleNameExceptMe(5, "Manager")).ReturnsAsync(false);

            var result = await sut.CheckDulicateExceptMe(5, "Manager");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(false, GetValue(json, "Result"));
        }

        [Fact]
        public async Task CheckDulicateExceptMe_ReturnsFalse_WhenRoleNameIsEmpty()
        {
            var result = await CreateSut().CheckDulicateExceptMe(5, "");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(false, GetValue(json, "Result"));
            _userRoleMock.Verify(x => x.CheckDuplicateRoleNameExceptMe(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        // ---------- CheckDulicate ----------

        [Fact]
        public async Task CheckDulicate_ReturnsTrue_WhenDuplicateFound()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.CheckDuplicateRoleName("Manager")).ReturnsAsync(true);

            var result = await sut.CheckDulicate("Manager");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(true, GetValue(json, "Result"));
        }

        [Fact]
        public async Task CheckDulicate_ReturnsFalse_WhenRoleNameIsEmpty()
        {
            var result = await CreateSut().CheckDulicate("");

            var json = Assert.IsType<JsonResult>(result);
            Assert.Equal(false, GetValue(json, "Result"));
            _userRoleMock.Verify(x => x.CheckDuplicateRoleName(It.IsAny<string>()), Times.Never);
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_ReturnsSuccess_WhenRoleIsDeleted()
        {
            var sut = CreateSut();
            _serviceMock.Setup(x => x.GetUserDataFromSession()).Returns(new UserViewModel { Id = 1 });
            _userRoleMock.Setup(x => x.DeleteRolebyID(5)).ReturnsAsync(true);

            var result = await sut.Delete(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(true, GetValue(ok, "Result"));
        }

        [Fact]
        public async Task Delete_ReturnsFailureMessage_WhenDeleteFails()
        {
            var sut = CreateSut();
            _userRoleMock.Setup(x => x.DeleteRolebyID(5)).ReturnsAsync(false);

            var result = await sut.Delete(5);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Role delete failed.", GetValue(ok, "Message"));
        }
    }
}
