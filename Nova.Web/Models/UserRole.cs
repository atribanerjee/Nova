using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
        public async Task<UserRoleViewModel> GetRoleDetailByID(int RoleID)
        {
            UserRoleViewModel model = new UserRoleViewModel();
            try
            {
                var Entity = await Task.Run(() => _Db.Roles.Find(RoleID));
                if (Entity != null && Entity.Id > 0)
                {
                    model.Id = Entity.Id;
                    model.Rolename = Entity.Rolename;
                    model.IsDeleted = Entity.IsDeleted;
                }
            }
            catch (Exception Ex)
            {
                // WriteLog("HealthGauge.Web.Models.RoleModel - GetRoleDetailByID", Ex.Message);
            }
            return model;
        }
        public async Task<bool> UpdateRole(UserRoleViewModel model)
        {
            bool Result = false;
            try
            {
                if (!String.IsNullOrEmpty(model.Rolename))
                {
                    var Entity = await Task.Run(() => _Db.Roles.Find(model.Id));
                    if (Entity != null && Entity.Id > 0)
                    {
                        Entity.Rolename = model.Rolename;
                        //Entity.IsActive = model.IsActive;

                        _Db.Roles.Update(Entity);
                        _Db.SaveChanges();

                        Result = true;
                    }
                }
            }
            catch (Exception Ex)
            {
               // WriteLog("HealthGauge.Web.Models.RoleModel - GetRoleDetailByID", Ex.Message);
            }

            return Result;
        }

        public async Task<bool> DeleteRolebyID(int ID)
        {
            bool Result = false;
            try
            {
                var Entity = await Task.Run(() => _Db.Roles.Find(ID));
                if (Entity != null && Entity.Id > 0)
                {
                    Entity.IsDeleted = true;
                    _Db.Roles.Update(Entity);
                    await _Db.SaveChangesAsync();

                    Result = true;
                }
            }
            catch (Exception Ex)
            {
                // WriteLog("HealthGauge.Web.Models.RoleModel - DeleteStorebyID", Ex.Message);
            }

            return Result;
        }
        public async Task<List<SelectListItem>> GetAllRoleListAsDropdown()
        {
            List<SelectListItem> _List = new List<SelectListItem>();
            try
            {
                _List = await (from r in _Db.Roles
                         where !r.IsDeleted
                         orderby r.Id descending
                         select new SelectListItem
                         {
                             Value = r.Id.ToString(),
                             Text = r.Rolename
                         }).ToListAsync();
            }
            catch (Exception Ex)
            {
                
            }
            return _List;
        }
    }
}
