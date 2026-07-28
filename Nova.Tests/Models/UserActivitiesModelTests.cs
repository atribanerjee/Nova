using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nova.DB;
using Nova.DB.POCO;
using Nova.Tests.TestHelpers;
using Nova.Web.Models;
using Nova.Web.Utitlity;
using Xunit;

namespace Nova.Tests.Models
{
    public class UserActivitiesModelTests
    {
        private readonly Mock<IUtilityServices> _utilityMock = new();
        private readonly NovaDBContext _db = DbContextFactory.CreateContext();

        private UserActivitiesModel CreateSut() =>
            new(_db, _utilityMock.Object, NullLogger<UserActivitiesModel>.Instance);

        private Users SeedUser(string username = "jdoe", bool isDeleted = false)
        {
            var role = new Roles { Rolename = "Admin", IsDeleted = false };
            _db.Roles.Add(role);
            _db.SaveChanges();

            var user = new Users
            {
                Firstname = "John",
                Lastname = "Doe",
                Username = username,
                Password = "hash",
                Email = $"{username}@example.com",
                RoleId = role.Id,
                IsActive = true,
                IsDeleted = isDeleted,
                CreatedDate = DateTime.UtcNow
            };
            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        [Fact]
        public async Task SaveActivity_PersistsActivity_ForUser()
        {
            var user = SeedUser();

            var result = await CreateSut().SaveActivity(user.Id, "User logged in");

            Assert.True(result);
            var stored = Assert.Single(_db.UserActivities);
            Assert.Equal(user.Id, stored.UserId);
            Assert.Equal("User logged in", stored.Description);
        }

        [Fact]
        public async Task GetUserActivities_ReturnsOnlyActivitiesForGivenUser()
        {
            var user1 = SeedUser("jdoe");
            var user2 = SeedUser("asmith");
            await CreateSut().SaveActivity(user1.Id, "User 1 activity");
            await CreateSut().SaveActivity(user2.Id, "User 2 activity");

            var result = await CreateSut().GetUserActivities(user1.Id);

            var activity = Assert.Single(result);
            Assert.Equal("User 1 activity", activity.Description);
            Assert.Equal("John Doe", activity.FullName);
        }

        [Fact]
        public async Task GetUserActivities_ReturnsAllActivities_WhenUserIdIsNull()
        {
            var user1 = SeedUser("jdoe");
            var user2 = SeedUser("asmith");
            await CreateSut().SaveActivity(user1.Id, "User 1 activity");
            await CreateSut().SaveActivity(user2.Id, "User 2 activity");

            var result = await CreateSut().GetUserActivities(null);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetUserActivities_ExcludesActivitiesForDeletedUsers()
        {
            var user = SeedUser("ghost", isDeleted: true);
            await CreateSut().SaveActivity(user.Id, "Ghost activity");

            var result = await CreateSut().GetUserActivities(null);

            Assert.Empty(result);
        }
    }
}
