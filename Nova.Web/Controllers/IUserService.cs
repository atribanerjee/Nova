using Nova.Web.Models;

namespace Nova.Web.Controllers
{
    public interface IUserService
    {
        Task<UserViewModel>  logins(UserViewModel model);
    }
}
