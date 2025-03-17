using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nova.Web.Models;

namespace Nova.Web.Controllers
{
    public interface IUserService
    {
        Task<UserViewModel>  logins(UserViewModel model);
        Task<UserViewModel> CheckEmailIDExit(string EmailID);
        public Task<bool> SaveGuid(string guid, Int32 id);
        public Task<UserViewModel> GetUserDetailByGUID(string guid);
        public  Task<bool> UpdatepasswordforUser(int? uid, string password);
        public  Task<UserViewModel> GetUsersDetails(Int32 ID);
        public Task<Boolean> CheckPassword(int? userid, string password);
        public void SetallSession(UserViewModel uvm);
        public object GetSessionValue(string sKey);
    }
}
