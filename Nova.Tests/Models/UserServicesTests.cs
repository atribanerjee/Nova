using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nova.DB;
using Nova.DB.POCO;
using Nova.Tests.TestHelpers;
using Nova.Web.Models;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using Xunit;

namespace Nova.Tests.Models
{
    public class UserServicesTests
    {
        private readonly Mock<IUtilityServices> _utilityMock = new();
        private readonly Mock<IPasswordHasherService> _hasherMock = new();
        private readonly NovaDBContext _db = DbContextFactory.CreateContext();

        private UserServices CreateSut() =>
            new(_db, _utilityMock.Object, _hasherMock.Object, NullLogger<UserServices>.Instance);

        private void SetupSession(int id = 1, string username = "admin", string firstname = "Ada",
            string lastname = "Admin", string email = "ada@example.com", int roleId = 1, string rolename = "Admin")
        {
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInUserID")).Returns(id.ToString());
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInUserName")).Returns(username);
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInFirstName")).Returns(firstname);
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInLastName")).Returns(lastname);
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInFullName")).Returns($"{firstname} {lastname}");
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInEmail")).Returns(email);
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInRoleID")).Returns(roleId.ToString());
            _utilityMock.Setup(x => x.GetSessionValue("LoggedInRolename")).Returns(rolename);
        }

        private Roles SeedRole(int id = 1, string name = "Admin")
        {
            var role = new Roles { Id = id, Rolename = name, IsDeleted = false };
            _db.Roles.Add(role);
            _db.SaveChanges();
            return role;
        }

        private Users SeedUser(Roles role, string username = "jdoe", string password = "hashed-pw",
            bool isActive = true, bool isDeleted = false, string email = "jdoe@example.com")
        {
            var user = new Users
            {
                Firstname = "John",
                Lastname = "Doe",
                Username = username,
                Password = password,
                Email = email,
                RoleId = role.Id,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreatedDate = DateTime.UtcNow
            };
            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        // ---------- CheckLogin ----------

        [Fact]
        public async Task CheckLogin_ReturnsUser_WhenCredentialsAreValid()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            bool needsRehash = false;
            _hasherMock.Setup(x => x.VerifyPassword("hashed-pw", "plainPwd", out needsRehash))
                .Returns(true);

            var result = await CreateSut().CheckLogin(new UserViewModel { Username = "jdoe", Password = "plainPwd" });

            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Admin", result.Rolename);
        }

        [Fact]
        public async Task CheckLogin_ReturnsEmptyModel_WhenPasswordIsInvalid()
        {
            var role = SeedRole();
            SeedUser(role);
            bool needsRehash = false;
            _hasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), out needsRehash))
                .Returns(false);

            var result = await CreateSut().CheckLogin(new UserViewModel { Username = "jdoe", Password = "wrong" });

            Assert.Equal(0, result.Id);
        }

        [Fact]
        public async Task CheckLogin_ReturnsEmptyModel_WhenUserDoesNotExist()
        {
            var result = await CreateSut().CheckLogin(new UserViewModel { Username = "ghost", Password = "x" });

            Assert.Equal(0, result.Id);
            bool needsRehash;
            _hasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), out needsRehash), Times.Never);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public async Task CheckLogin_ReturnsEmptyModel_WhenUserIsInactiveOrDeleted(bool isActive, bool isDeleted)
        {
            var role = SeedRole();
            SeedUser(role, isActive: isActive, isDeleted: isDeleted);

            var result = await CreateSut().CheckLogin(new UserViewModel { Username = "jdoe", Password = "plainPwd" });

            Assert.Equal(0, result.Id);
        }

        [Fact]
        public async Task CheckLogin_RehashesPassword_WhenHasherSignalsNeedsRehash()
        {
            var role = SeedRole();
            SeedUser(role);
            bool needsRehash = true;
            _hasherMock.Setup(x => x.VerifyPassword("hashed-pw", "plainPwd", out needsRehash))
                .Returns(true);
            _hasherMock.Setup(x => x.HashPassword("plainPwd")).Returns("new-hash");

            await CreateSut().CheckLogin(new UserViewModel { Username = "jdoe", Password = "plainPwd" });

            var stored = _db.Users.Single(u => u.Username == "jdoe");
            Assert.Equal("new-hash", stored.Password);
        }

        // ---------- CheckEmailExists ----------

        [Fact]
        public async Task CheckEmailExists_ReturnsUser_WhenEmailMatchesCaseInsensitively()
        {
            var role = SeedRole();
            SeedUser(role, email: "Jdoe@Example.com");

            var result = await CreateSut().CheckEmailExists("  jdoe@example.com  ");

            Assert.NotEqual(0, result.Id);
        }

        [Fact]
        public async Task CheckEmailExists_ReturnsEmptyModel_WhenNoMatch()
        {
            var result = await CreateSut().CheckEmailExists("nobody@example.com");

            Assert.Equal(0, result.Id);
        }

        // ---------- SaveGuid ----------

        [Fact]
        public async Task SaveGuid_SetsResetTokenAndExpiry_ForActiveUser()
        {
            var role = SeedRole();
            var user = SeedUser(role);

            var result = await CreateSut().SaveGuid("guid-123", user.Id);

            Assert.True(result);
            var stored = _db.Users.Single(u => u.Id == user.Id);
            Assert.Equal("guid-123", stored.ResetPasswordToken);
            Assert.True(stored.ResetPasswordTokenExpiry > DateTime.UtcNow);
        }

        [Fact]
        public async Task SaveGuid_ReturnsFalse_WhenUserDoesNotExist()
        {
            var result = await CreateSut().SaveGuid("guid-123", 999);

            Assert.False(result);
        }

        // ---------- GetUserDetailByGUID ----------

        [Fact]
        public async Task GetUserDetailByGUID_ReturnsUser_ForValidUnexpiredToken()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.ResetPasswordToken = "valid-guid";
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            _db.SaveChanges();

            var result = await CreateSut().GetUserDetailByGUID("valid-guid");

            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task GetUserDetailByGUID_ReturnsEmptyModel_ForExpiredToken()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.ResetPasswordToken = "expired-guid";
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddMinutes(-5);
            _db.SaveChanges();

            var result = await CreateSut().GetUserDetailByGUID("expired-guid");

            Assert.Equal(0, result.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task GetUserDetailByGUID_ReturnsEmptyModel_ForNullOrEmptyGuid(string? guid)
        {
            var result = await CreateSut().GetUserDetailByGUID(guid!);

            Assert.Equal(0, result.Id);
        }

        // ---------- UpdatePasswordForUser ----------

        [Fact]
        public async Task UpdatePasswordForUser_UpdatesHashAndClearsResetToken()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.ResetPasswordToken = "some-guid";
            user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);
            _db.SaveChanges();
            _hasherMock.Setup(x => x.HashPassword("NewPass123")).Returns("new-hashed");

            var result = await CreateSut().UpdatePasswordForUser(user.Id, "NewPass123");

            Assert.True(result);
            var stored = _db.Users.Single(u => u.Id == user.Id);
            Assert.Equal("new-hashed", stored.Password);
            Assert.Null(stored.ResetPasswordToken);
            Assert.Null(stored.ResetPasswordTokenExpiry);
        }

        [Fact]
        public async Task UpdatePasswordForUser_ReturnsFalse_WhenUserNotFound()
        {
            var result = await CreateSut().UpdatePasswordForUser(999, "NewPass123");

            Assert.False(result);
        }

        // ---------- CheckPassword ----------

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CheckPassword_ReturnsFalse_ForNullOrNonPositiveUserId(int? userId)
        {
            var result = await CreateSut().CheckPassword(userId, "any");

            Assert.False(result);
        }

        [Fact]
        public async Task CheckPassword_ReturnsTrue_WhenHasherConfirmsMatch()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            bool needsRehash = false;
            _hasherMock.Setup(x => x.VerifyPassword("hashed-pw", "plainPwd", out needsRehash))
                .Returns(true);

            var result = await CreateSut().CheckPassword(user.Id, "plainPwd");

            Assert.True(result);
        }

        [Fact]
        public async Task CheckPassword_ReturnsFalse_WhenUserNotFound()
        {
            var result = await CreateSut().CheckPassword(999, "plainPwd");

            Assert.False(result);
        }

        // ---------- GetAllUsersList ----------

        [Fact]
        public async Task GetAllUsersList_ExcludesDeletedUsers()
        {
            var role = SeedRole();
            SeedUser(role, username: "active-user");
            SeedUser(role, username: "deleted-user", isDeleted: true);

            var result = await CreateSut().GetAllUsersList(new UserViewModel());

            Assert.Single(result);
            Assert.Equal("active-user", result[0].Username);
        }

        // ---------- GetUserDetailByUserID ----------

        [Fact]
        public async Task GetUserDetailByUserID_ReturnsUser_WhenFound()
        {
            var role = SeedRole();
            var user = SeedUser(role);

            var result = await CreateSut().GetUserDetailByUserID(user.Id);

            Assert.Equal(user.Id, result.Id);
            Assert.Equal("Admin", result.Rolename);
        }

        [Fact]
        public async Task GetUserDetailByUserID_ReturnsEmptyModel_WhenIdIsNotPositive()
        {
            var result = await CreateSut().GetUserDetailByUserID(0);

            Assert.Equal(0, result.Id);
        }

        [Fact]
        public async Task GetUserDetailByUserID_ReturnsEmptyModel_WhenNotFound()
        {
            var result = await CreateSut().GetUserDetailByUserID(999);

            Assert.Equal(0, result.Id);
        }

        // ---------- CheckDuplicateEmail / CheckDuplicateUsername ----------

        [Fact]
        public async Task CheckDuplicateEmail_ReturnsTrue_WhenEmailAlreadyUsed()
        {
            var role = SeedRole();
            SeedUser(role, email: "taken@example.com");

            var result = await CreateSut().CheckDuplicateEmail("taken@example.com", null);

            Assert.True(result);
        }

        [Fact]
        public async Task CheckDuplicateEmail_ExcludesGivenUserId()
        {
            var role = SeedRole();
            var user = SeedUser(role, email: "self@example.com");

            var result = await CreateSut().CheckDuplicateEmail("self@example.com", user.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task CheckDuplicateUsername_ReturnsTrue_WhenUsernameAlreadyUsed()
        {
            var role = SeedRole();
            SeedUser(role, username: "jdoe");

            var result = await CreateSut().CheckDuplicateUsername("jdoe", null);

            Assert.True(result);
        }

        [Fact]
        public async Task CheckDuplicateUsername_ExcludesGivenUserId()
        {
            var role = SeedRole();
            var user = SeedUser(role, username: "jdoe");

            var result = await CreateSut().CheckDuplicateUsername("jdoe", user.Id);

            Assert.False(result);
        }

        // ---------- UpdateUser ----------

        [Fact]
        public async Task UpdateUser_UpdatesChangedFields()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            SetupSession(id: 42);

            var result = await CreateSut().UpdateUser(new UserViewModel
            {
                Id = user.Id,
                Firstname = "Jonathan",
                Lastname = "Doe",
                RoleId = role.Id,
                Phone = "555-1234"
            });

            Assert.Equal(user.Id, result);
            var stored = _db.Users.Single(u => u.Id == user.Id);
            Assert.Equal("Jonathan", stored.Firstname);
            Assert.Equal("555-1234", stored.Phone);
            Assert.Equal(42, stored.ModifiedBy);
        }

        [Fact]
        public async Task UpdateUser_ReturnsZero_WhenUserNotFound()
        {
            SetupSession();

            var result = await CreateSut().UpdateUser(new UserViewModel { Id = 999, Firstname = "X", Lastname = "Y" });

            Assert.Equal(0, result);
        }

        // ---------- DeleteUserByUserID ----------

        [Fact]
        public async Task DeleteUserByUserID_SoftDeletesUser()
        {
            var role = SeedRole();
            var user = SeedUser(role);

            var result = await CreateSut().DeleteUserByUserID(user.Id);

            Assert.True(result);
            Assert.True(_db.Users.Single(u => u.Id == user.Id).IsDeleted);
        }

        [Fact]
        public async Task DeleteUserByUserID_ReturnsFalse_WhenUserNotFound()
        {
            var result = await CreateSut().DeleteUserByUserID(999);

            Assert.False(result);
        }

        // ---------- StatusUpdateForUserByUserID ----------

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StatusUpdateForUserByUserID_UpdatesActiveFlag(bool status)
        {
            var role = SeedRole();
            var user = SeedUser(role);

            var result = await CreateSut().StatusUpdateForUserByUserID(user.Id, status);

            Assert.True(result);
            Assert.Equal(status, _db.Users.Single(u => u.Id == user.Id).IsActive);
        }

        [Fact]
        public async Task StatusUpdateForUserByUserID_ReturnsFalse_WhenUserNotFound()
        {
            var result = await CreateSut().StatusUpdateForUserByUserID(999, true);

            Assert.False(result);
        }

        // ---------- LogOut ----------

        [Fact]
        public void LogOut_DelegatesToUtilityServices()
        {
            CreateSut().LogOut();

            _utilityMock.Verify(x => x.LogOut(), Times.Once);
        }

        // ---------- Generate2FACode ----------

        [Fact]
        public async Task Generate2FACode_SetsCodeAndSendsEmail_ForActiveUser()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            _utilityMock.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(true);

            var result = await CreateSut().Generate2FACode(user.Id);

            Assert.True(result);
            var stored = _db.Users.Single(u => u.Id == user.Id);
            Assert.False(string.IsNullOrEmpty(stored.TwoFactorCode));
            Assert.Equal(6, stored.TwoFactorCode!.Length);
            Assert.True(stored.TwoFactorCodeExpiry > DateTime.UtcNow);
            _utilityMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), user.Email, "2FA.html", It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        [Fact]
        public async Task Generate2FACode_ReturnsFalse_WhenUserNotFound()
        {
            var result = await CreateSut().Generate2FACode(999);

            Assert.False(result);
            _utilityMock.Verify(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        // ---------- Check2FACode ----------

        [Fact]
        public async Task Check2FACode_ReturnsTrue_AndClearsCode_ForValidUnexpiredCode()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.TwoFactorCode = "123456";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(2);
            _db.SaveChanges();

            var result = await CreateSut().Check2FACode(user.Id, "123456");

            Assert.True(result);
            var stored = _db.Users.Single(u => u.Id == user.Id);
            Assert.Null(stored.TwoFactorCode);
            Assert.Null(stored.TwoFactorCodeExpiry);
            _utilityMock.Verify(x => x.SetSessionValue("LoggedInUserID", user.Id), Times.Once);
        }

        [Fact]
        public async Task Check2FACode_ReturnsFalse_ForExpiredCode()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.TwoFactorCode = "123456";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-1);
            _db.SaveChanges();

            var result = await CreateSut().Check2FACode(user.Id, "123456");

            Assert.False(result);
        }

        [Fact]
        public async Task Check2FACode_ReturnsFalse_ForMismatchedCode()
        {
            var role = SeedRole();
            var user = SeedUser(role);
            user.TwoFactorCode = "123456";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(2);
            _db.SaveChanges();

            var result = await CreateSut().Check2FACode(user.Id, "000000");

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Check2FACode_ReturnsFalse_ForMissingCode(string? code)
        {
            var result = await CreateSut().Check2FACode(1, code!);

            Assert.False(result);
        }

        // ---------- SaveUser ----------

        [Fact]
        public async Task SaveUser_CreatesUser_WithHashedPasswordAndAuditFields()
        {
            SeedRole();
            SetupSession(id: 7);
            _hasherMock.Setup(x => x.HashPassword("Passw0rd!")).Returns("hashed-value");

            var result = await CreateSut().SaveUser(new UserViewModel
            {
                Firstname = "New",
                Lastname = "User",
                Email = "NEW.USER@Example.com",
                Username = "newuser",
                NewPassword = "Passw0rd!",
                RoleId = 1
            });

            Assert.True(result);
            var stored = _db.Users.Single(u => u.Username == "newuser");
            Assert.Equal("hashed-value", stored.Password);
            Assert.Equal("new.user@example.com", stored.Email);
            Assert.Equal(7, stored.CreatedBy);
            Assert.True(stored.IsActive);
            Assert.False(stored.IsDeleted);
        }
    }
}
