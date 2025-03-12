using Nova.DB;
using Nova.DB.Utitlity;
using Nova.Web.Models;
using Nova.Web.Utitlity;

namespace Nova.Web.Controllers
{
    public class UserService:IUserService
    {
        public Nova.DB.NovaDBContext _Db;
        private IUtilityService _Utility;
        public UserService(NovaDBContext Db, IUtilityService Uti)
        {
            _Db = Db;
            _Utility = Uti;
        }
        public async Task<UserViewModel>  logins(UserViewModel model)
        {
            bool result = false;

            UserViewModel UVM = new UserViewModel();

            string password =await _Utility.sha256encription(model.Password);
            UVM = (from u in _Db.Users
                              where u.Username == model.Username && u.Password == password
                   select new UserViewModel
                              {
                                  Id = u.Id,
                                  Username = u.Username ?? string.Empty,
                                  Firstname = u.Firstname ?? string.Empty,
                                  Lastname = u.Lastname ?? string.Empty,
                                  Email = u.Email ?? string.Empty,
                                  RoleId = u.RoleId,
                              }).FirstOrDefault();

            if (UVM != null && UVM.Id > 0)
            {
                result = true;
                _Utility.SetSessionValue("LoggedInUserID", UVM.Id);
                _Utility.SetSessionValue("LoggedInUserName", UVM.Username);
                

                return UVM;
            }

            return new UserViewModel();
        }


    }
}
