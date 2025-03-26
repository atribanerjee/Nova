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
        public UserViewModel GetUserDataFromSession();
        Task<Boolean> CheckPassword(int? userid, string password);
        Task<List<UserViewModel>> GetAllUsersList(UserViewModel UVM);
        Task<UserViewModel> GetUserDetailByUserID(int UserID);
        Task<bool> CheckDuplicateEmail(string EmailID, int userid);
        Task<bool> CheckDuplicateUsername(string userName, int userid);
        Task<Int32> UpdateUser(UserViewModel model);
        Task<bool> DeleteUserByUserID(int UserID);
        public void LogOut();
        public Task<bool> Generate2FACode(int id);
        public Task<bool> Check2FACode(int id, string twoFactorCode);
        public Task<bool> StatusUpdateForUserByUserID(int userId, bool status);
    }
}
