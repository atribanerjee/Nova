using Nova.DB;
using Nova.Web.Models;

namespace Nova.Web.Controllers
{
    public class UserService:IUserService
    {
        public Nova.DB.NovaDBContext _Db;
        public UserService(NovaDBContext Db)
        {
            _Db = Db;
        }
        public async Task<UserViewModel>  logins(UserViewModel model)
        {
            bool result = false;

            UserViewModel UVM = new UserViewModel();

           // string password = new UtilityHelper().sha256encription(model.password);
            UVM = (from u in _Db.Users
                              where u.Username == model.Username && u.Password == model.Password
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
                //new UtilityHelper().SetSessionValue("LoggedInUserID", userloginmodel.id);
                //new UtilityHelper().SetSessionValue("LoggedInUserName", userloginmodel.username);

                return UVM;
            }

            return new UserViewModel();
        }


    }
}
