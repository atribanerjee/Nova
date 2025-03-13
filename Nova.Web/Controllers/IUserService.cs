using Nova.Web.Models;

namespace Nova.Web.Controllers
{
    public interface IUserService
    {
        Task<UserViewModel>  logins(UserViewModel model);
        Task<UserViewModel> CheckEmailIDExit(string EmailID);
        public Task<bool> SaveGuid(string guid, Int32 id);
        public Task<UserViewModel> GetUserDetailByGUID(string guid);
        public  Task<bool> UpdatepasswordforUser(string? uid, string password);
    }
}
