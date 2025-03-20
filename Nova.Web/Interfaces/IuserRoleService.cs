using Nova.Web.ViewModels;

namespace Nova.Web.Interfaces
{
    public interface IuserRoleService
    {
        Task<List<UserRoleViewModel>> GetAllRoleList(UserRoleViewModel RVM, string searchvalue);
        Task<Boolean> CheckDuplicateRoleName(String RoleName);
        Task<Boolean> AddNewRole(UserRoleViewModel model);
        Task<Boolean> CheckDuplicateRoleNameExceptMe(int RoleID, String RoleName);
        Task<UserRoleViewModel> GetRoleDetailByID(int RoleID);
        Task<bool> UpdateRole(UserRoleViewModel model);
        Task<bool> DeleteRolebyID(int ID);
    }
}
