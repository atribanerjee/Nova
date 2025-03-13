using Nova.DB;
using Nova.DB.Utitlity;
using Nova.Web.Models;
using Nova.Web.Utitlity;

namespace Nova.Web.Controllers
{
    public class UserService : IUserService
    {
        public Nova.DB.NovaDBContext _Db;
        private IUtilityService _Utility;
        public UserService(NovaDBContext Db, IUtilityService Uti)
        {
            _Db = Db;
            _Utility = Uti;
        }
        public async Task<UserViewModel> logins(UserViewModel model)
        {
            UserViewModel UVM = new UserViewModel();

            string password = await _Utility.Encrypt(model.Password);
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
                   }).FirstOrDefault() ?? new UserViewModel();

            if (UVM != null && UVM.Id > 0)
            {
                await SetallSession(UVM);
                return UVM;
            }

            return new UserViewModel();
        }

        public async Task SetallSession(UserViewModel UVM)
        {
            
            await _Utility.SetSessionValue("LoggedInUserID", UVM.Id);
            await _Utility.SetSessionValue("LoggedInUserName", UVM.Username);
            await _Utility.SetSessionValue("LoggedInFirstName", UVM.Firstname);
            await _Utility.SetSessionValue("LoggedInLastName", UVM.Lastname);
            await _Utility.SetSessionValue("LoggedInEmail", UVM.Email);
           
        }

        //public async Task<UserViewModel> Register(UserViewModel model)
        //{
        //    UserViewModel UVM = new UserViewModel();
        //    Users user = new Users();
        //    user.Firstname = model.Firstname;
        //    user.Lastname = model.Lastname;
        //    user.Username = model.Username;
        //    user.Password = await _Utility.Encrypt(model.Password);
        //    user.Email = model.Email;
        //    user.RoleId = model.RoleId;
        //    user.CreatedDate = DateTime.Now;
        //    user.CreatedBy = 1;
        //    user.IsActive = true;
        //    user.IsDeleted = false;
        //    _Db.Users.Add(user);
        //    await _Db.SaveChangesAsync();
        //    UVM.Id = user.Id;
        //    UVM.Firstname = user.Firstname;
        //    UVM.Lastname = user.Lastname;
        //    UVM.Username = user.Username;
        //    UVM.Email = user.Email;
        //    UVM.RoleId = user.RoleId;
        //    return UVM;
        //}
    }
}
