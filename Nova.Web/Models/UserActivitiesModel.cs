using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.DB.POCO;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;

namespace Nova.Web.Models
{
    public class UserActivitiesModel: IUserActivities
    {
        public NovaDBContext _Db;
        private IUtilityServices _Utility;
        private ILogger<UserActivitiesModel> _logger;
        public UserActivitiesModel(NovaDBContext Db, IUtilityServices Utility, ILogger<UserActivitiesModel> logger)
        {
            _Db = Db;
            _Utility = Utility;
            _logger = logger;
        }

        public async Task<bool> SaveActivity(int UserId,string Description)
        {
            try
            {
                var UserActivity = new UserActivities
                {
                    UserId = UserId,
                    Description = Description,
                    CreatedDate = DateTime.Now
                };

                _Db.UserActivities.Add(UserActivity);
                await _Db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                                _logger.LogError(ex, "Failed to save activity for user {UserId}.", UserId);
            }
            return false;
        }

        public async Task<List<ActivityViewModel>> GetUserActivities(int? UserId)
        {
            List<ActivityViewModel> _List = new List<ActivityViewModel>();
            try
            {
                if (UserId != null && UserId > 0)
                {
                    _List = await (from ua in _Db.UserActivities
                                   where ua.UserId == UserId && !ua.User.IsDeleted
                                   orderby ua.Id descending
                                   select new ActivityViewModel
                                   {
                                       Id = ua.Id,
                                       UserId = ua.UserId,
                                       FullName = ua.User.Firstname + " " + ua.User.Lastname,
                                       Username = ua.User.Username,
                                       Description = ua.Description,
                                       CreatedDate = ua.CreatedDate
                                   }).ToListAsync();
                }
                else
                {
                    _List = await (from ua in _Db.UserActivities
                                   where !ua.User.IsDeleted
                                   orderby ua.Id descending
                                   select new ActivityViewModel
                                   {
                                       Id = ua.Id,
                                       UserId = ua.UserId,
                                       FullName = ua.User.Firstname + " " + ua.User.Lastname,
                                       Username = ua.User.Username,
                                       Description = ua.Description,
                                       CreatedDate = ua.CreatedDate
                                   }).ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user activities for user {UserId}.", UserId);
            }
            return _List;
        }
    }
}
