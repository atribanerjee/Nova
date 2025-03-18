using Nova.Web.ViewModels;

namespace Nova.Web.Interfaces
{
    public interface IuserRoleService
    {
        Task<List<UserRoleViewModel>> GetAllRoleList(UserRoleViewModel RVM, string searchvalue);
        
    }
}
