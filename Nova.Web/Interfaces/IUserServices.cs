using Nova.Web.ViewModels;

namespace Nova.Web.Interfaces
{
    public interface IUserServices
    {
        public Task<UserViewModel> CheckLogin(UserViewModel model);
        public Task<UserViewModel> CheckEmailExists(string EmailID);
        public Task<bool> SaveGuid(string guid, int UserID);
        public Task<UserViewModel> GetUserDetailByGUID(string guid);
        public Task<bool> UpdatePasswordForUser(int UserID, string Password);
        public Task<UserViewModel> GetUsersDetailsByID(int UserID);        
        public UserViewModel GetUserDataFromSession();
        Task<Boolean> CheckPassword(int? userid, string password);
        public void LogOut();
    }
}
