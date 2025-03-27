using Nova.Web.ViewModels;

namespace Nova.Web.Interfaces
{
    public interface IUserActivities
    {
        public Task<bool> SaveActivity(int UserId, string Description);
        public Task<List<ActivityViewModel>> GetUserActivities(int? UserId);
    }
}
