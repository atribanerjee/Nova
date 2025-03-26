using Nova.DB;
using Nova.DB.POCO;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;

namespace Nova.Web.Models
{
    public class UserActivitiesModel: IUserActivities
    {
        public NovaDBContext _Db;
        private IUtilityServices _Utility;
        public UserActivitiesModel(NovaDBContext Db, IUtilityServices Utility)
        {
            _Db = Db;
            _Utility = Utility;
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
            catch (Exception Ex)
            {

            }
            return false;
        }
    }
}
