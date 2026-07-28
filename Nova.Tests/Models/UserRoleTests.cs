using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nova.DB.POCO;
using Nova.Tests.TestHelpers;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using Xunit;
using UserRoleService = Nova.Web.Models.UserRole;

namespace Nova.Tests.Models
{
    public class UserRoleTests
    {
        private readonly Mock<IUtilityServices> _utilityMock = new();
        private readonly Nova.DB.NovaDBContext _db = DbContextFactory.CreateContext();

        private UserRoleService CreateSut() =>
            new(_db, _utilityMock.Object, NullLogger<UserRoleService>.Instance);

        private Roles SeedRole(string name = "Manager", bool isDeleted = false)
        {
            var role = new Roles { Rolename = name, IsDeleted = isDeleted };
            _db.Roles.Add(role);
            _db.SaveChanges();
            return role;
        }

        // ---------- GetAllRoleList ----------

        [Fact]
        public async Task GetAllRoleList_ExcludesDeletedRoles()
        {
            SeedRole("Manager");
            SeedRole("Retired", isDeleted: true);

            var result = await CreateSut().GetAllRoleList(new UserRoleViewModel(), "");

            Assert.Single(result);
            Assert.Equal("Manager", result[0].Rolename);
        }

        [Fact]
        public async Task GetAllRoleList_FiltersBySearchValue()
        {
            SeedRole("Manager");
            SeedRole("Administrator");

            var result = await CreateSut().GetAllRoleList(new UserRoleViewModel(), "Admin");

            Assert.Single(result);
            Assert.Equal("Administrator", result[0].Rolename);
        }

        // ---------- CheckDuplicateRoleName ----------

        [Fact]
        public async Task CheckDuplicateRoleName_ReturnsTrue_CaseInsensitively()
        {
            SeedRole("Manager");

            var result = await CreateSut().CheckDuplicateRoleName("manager");

            Assert.True(result);
        }

        [Fact]
        public async Task CheckDuplicateRoleName_ReturnsFalse_WhenNoMatch()
        {
            var result = await CreateSut().CheckDuplicateRoleName("Nonexistent");

            Assert.False(result);
        }

        [Fact]
        public async Task CheckDuplicateRoleName_IgnoresDeletedRoles()
        {
            SeedRole("Manager", isDeleted: true);

            var result = await CreateSut().CheckDuplicateRoleName("Manager");

            Assert.False(result);
        }

        // ---------- AddNewRole ----------

        [Fact]
        public async Task AddNewRole_AddsRole_WhenNameProvided()
        {
            var result = await CreateSut().AddNewRole(new UserRoleViewModel { Rolename = "NewRole" });

            Assert.True(result);
            Assert.Contains(_db.Roles, r => r.Rolename == "NewRole" && !r.IsDeleted);
        }

        [Fact]
        public async Task AddNewRole_ReturnsFalse_WhenNameIsEmpty()
        {
            var result = await CreateSut().AddNewRole(new UserRoleViewModel { Rolename = "" });

            Assert.False(result);
            Assert.Empty(_db.Roles);
        }

        // ---------- CheckDuplicateRoleNameExceptMe ----------

        [Fact]
        public async Task CheckDuplicateRoleNameExceptMe_ExcludesGivenRoleId()
        {
            var role = SeedRole("Manager");

            var result = await CreateSut().CheckDuplicateRoleNameExceptMe(role.Id, "Manager");

            Assert.False(result);
        }

        [Fact]
        public async Task CheckDuplicateRoleNameExceptMe_ReturnsTrue_ForDifferentRoleWithSameName()
        {
            var role = SeedRole("Manager");
            SeedRole("Supervisor");

            var result = await CreateSut().CheckDuplicateRoleNameExceptMe(role.Id, "Supervisor");

            Assert.True(result);
        }

        // ---------- GetRoleDetailByID ----------

        [Fact]
        public async Task GetRoleDetailByID_ReturnsRole_WhenFound()
        {
            var role = SeedRole("Manager");

            var result = await CreateSut().GetRoleDetailByID(role.Id);

            Assert.Equal(role.Id, result.Id);
            Assert.Equal("Manager", result.Rolename);
        }

        [Fact]
        public async Task GetRoleDetailByID_ReturnsEmptyModel_WhenNotFound()
        {
            var result = await CreateSut().GetRoleDetailByID(999);

            Assert.Equal(0, result.Id);
        }

        // ---------- UpdateRole ----------

        [Fact]
        public async Task UpdateRole_UpdatesName_WhenFound()
        {
            var role = SeedRole("Manager");

            var result = await CreateSut().UpdateRole(new UserRoleViewModel { Id = role.Id, Rolename = "Senior Manager" });

            Assert.True(result);
            Assert.Equal("Senior Manager", _db.Roles.Single(r => r.Id == role.Id).Rolename);
        }

        [Fact]
        public async Task UpdateRole_ReturnsFalse_WhenNotFound()
        {
            var result = await CreateSut().UpdateRole(new UserRoleViewModel { Id = 999, Rolename = "X" });

            Assert.False(result);
        }

        // ---------- DeleteRolebyID ----------

        [Fact]
        public async Task DeleteRolebyID_SoftDeletesRole()
        {
            var role = SeedRole("Manager");

            var result = await CreateSut().DeleteRolebyID(role.Id);

            Assert.True(result);
            Assert.True(_db.Roles.Single(r => r.Id == role.Id).IsDeleted);
        }

        [Fact]
        public async Task DeleteRolebyID_ReturnsFalse_WhenNotFound()
        {
            var result = await CreateSut().DeleteRolebyID(999);

            Assert.False(result);
        }

        // ---------- GetAllRoleListAsDropdown ----------

        [Fact]
        public async Task GetAllRoleListAsDropdown_ExcludesDeletedRoles()
        {
            var active = SeedRole("Manager");
            SeedRole("Retired", isDeleted: true);

            var result = await CreateSut().GetAllRoleListAsDropdown();

            var item = Assert.Single(result);
            Assert.Equal(active.Id.ToString(), item.Value);
            Assert.Equal("Manager", item.Text);
        }
    }
}
