using Microsoft.AspNetCore.Mvc.Rendering;
using Nova.Web.ViewModels;

namespace Nova.Web.Interfaces
{
    public interface IuserRoleService
    {
        public Task<List<UserRoleViewModel>> GetAllRoleList(UserRoleViewModel RVM, string searchvalue);
        public Task<Boolean> CheckDuplicateRoleName(String RoleName);
        public Task<Boolean> AddNewRole(UserRoleViewModel model);
        public Task<Boolean> CheckDuplicateRoleNameExceptMe(int RoleID, String RoleName);
        public Task<UserRoleViewModel> GetRoleDetailByID(int RoleID);
        public Task<bool> UpdateRole(UserRoleViewModel model);
        public Task<bool> DeleteRolebyID(int ID);
        public Task<List<SelectListItem>> GetAllRoleListAsDropdown();
    }
}
