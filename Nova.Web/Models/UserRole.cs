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

        public async Task<Boolean> CheckDuplicateRoleName(String RoleName)
        {
            try
            {
                if (!String.IsNullOrEmpty(RoleName))
                {
                    var entity = await Task.Run(() => _Db.Roles
                        .Where(x => x.Rolename.ToLower() == RoleName.ToLower() && !x.IsDeleted)
                        .FirstOrDefault());

                    if (entity != null && entity.Id > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception Ex)
            {
                
            }

            return false;
        }

        public async Task<Boolean> AddNewRole(UserRoleViewModel model)
        {
            bool Result = false;
            try
            {
                if (!String.IsNullOrEmpty(model.Rolename))
                {
                    var Entity = new Nova.DB.POCO.Roles();
                    Entity.Rolename = model.Rolename;
                    Entity.IsDeleted = false;
                    _Db.Roles.Add(Entity);
                    await _Db.SaveChangesAsync(); // Use await here
                    Result = true;
                }
            }
            catch (Exception Ex)
            {
            }

            return Result;
        }

        public async Task<Boolean> CheckDuplicateRoleNameExceptMe(int RoleID, String RoleName)
        {
            try
            {
                if (!String.IsNullOrEmpty(RoleName))
                {
                    var entity = await Task.Run(() => _Db.Roles
                        .Where(x => x.Rolename.ToLower() == RoleName.ToLower()
                                    && x.Id != RoleID
                                    && !x.IsDeleted)
                        .FirstOrDefault());

                    if (entity != null && entity.Id > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception Ex)
            {
                //WriteLog("HealthGauge.Web.Models.RoleModel - CheckDuplicateRoleName", Ex.Message);
            }

            return false;
        }
      

    }
}
