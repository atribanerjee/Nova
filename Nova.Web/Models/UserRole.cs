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
        private ILogger<UserRole> _logger;
        public UserRole(NovaDBContext Db, IUtilityServices Utility, ILogger<UserRole> logger)
        {
            _Db = Db;
            _Utility = Utility;
            _logger = logger;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all role list for search value {SearchValue}.", searchvalue);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check duplicate role name for role {RoleName}.", RoleName);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add new role with name {RoleName}.", model.Rolename);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check duplicate role name for role {RoleName}.", RoleName);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get role detail by ID {RoleID}.", RoleID);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update role with ID {RoleID}.", model.Id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete role with ID {RoleID}.", ID);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all role list as dropdown.");
            }
            return _List;
        }
    }
}
