using Nova.DB;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;

namespace Nova.Web.Models
{
    public class UserRole : IuserRoleService
    {
        public NovaDBContext _Db;
        private IUtilityServices _Utility;
        public UserRole(NovaDBContext Db, IUtilityServices Utility)
        {
            _Db = Db;
            _Utility = Utility;
        }
        public async Task<List<UserRoleViewModel>> GetAllRoleList(UserRoleViewModel UVM, string searchvalue)
        {
            List<UserRoleViewModel> _List = new List<UserRoleViewModel>();

            try
            {
                if (!String.IsNullOrEmpty(searchvalue))
                {
                    _List = await Task.Run(() => (from r in _Db.Roles
                                                  where !r.IsDeleted && (r.Rolename.Contains(searchvalue))
                                                  orderby r.Id descending
                                                  select new UserRoleViewModel
                                                  {
                                                      Id = r.Id,
                                                      Rolename = r.Rolename,
                                                  }).ToList());
                }
                else
                {
                    _List = await Task.Run(() => (from r in _Db.Roles
                                                  where !r.IsDeleted
                                                  orderby r.Id descending
                                                  select new UserRoleViewModel
                                                  {
                                                      Id = r.Id,
                                                      Rolename = r.Rolename,
                                                  }).ToList());
                }
            }
            catch (Exception Ex)
            {
                //WriteLog("HealthGauge.Web.Models.UserModel - GetAllUsersList", Ex.Message);
            }
            return _List;
        }
    }
}
