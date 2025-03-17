using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
                       Password = model.Password
                   }).FirstOrDefault() ?? new UserViewModel();

            if (UVM != null && UVM.Id > 0)
            {
                 SetallSession(UVM);
                return UVM;
            }

            return new UserViewModel();
        }

        public void  SetallSession(UserViewModel uvm)
        {

            _Utility.SetSessionValue("LoggedInUserID", uvm.Id);
             _Utility.SetSessionValue("LoggedInUserName", uvm.Username);
             _Utility.SetSessionValue("LoggedInFirstName", uvm.Firstname);
             _Utility.SetSessionValue("LoggedInLastName", uvm.Lastname);
             _Utility.SetSessionValue("LoggedInEmail", uvm.Email);
        }

        public object GetSessionValue(string sKey)
        {
          return  _Utility.GetSessionValue(sKey);
        }

        public async Task<UserViewModel> CheckEmailIDExit(string EmailID)
        {
            UserViewModel UVM = new UserViewModel();
            try
            {
                UVM = await Task.Run(() =>
                {
                    return (from u in _Db.Users
                            where u.Email.ToLower() == EmailID.Trim().ToLower() && !u.IsDeleted
                            select new UserViewModel
                            {
                                Username = u.Username,
                                Firstname = u.Firstname,
                                Lastname = u.Lastname,
                                Id = u.Id
                            }).FirstOrDefault() ?? new UserViewModel();
                });

                if (UVM == null)
                {
                    UVM = new UserViewModel();
                }
            }
            catch (Exception)
            {
                // Handle exception
            }
            return UVM;
        }

        public async Task<bool> SaveGuid(string guid, Int32 id)
        {
            bool Result = false;
            var entity = await _Db.Users.Where(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync();

            if (entity != null)
            {
                entity.ResetPasswordToken = guid;
                entity.ResetPasswordTokenExpiry = DateTime.Now;
                _Db.Users.Update(entity);
                await _Db.SaveChangesAsync();
                Result = true;
            }
            return Result;
        }

        public async Task<UserViewModel> GetUserDetailByGUID(string guid)
        {
            UserViewModel model = new UserViewModel();

            try
            {
                if (!string.IsNullOrEmpty(guid))
                {
                    model = await Task.Run(() =>
                    {
                        return (from u in _Db.Users
                                where !u.IsDeleted && u.ResetPasswordToken == guid
                                select new UserViewModel
                                {
                                    Id = u.Id,
                                    UserId = u.Id,
                                    Firstname = u.Firstname ?? string.Empty,
                                    Lastname = u.Lastname ?? string.Empty,
                                    Email = u.Email ?? string.Empty,
                                    Username = u.Username ?? string.Empty,
                                    CreatedDate = u.CreatedDate,
                                }).FirstOrDefault() ?? new UserViewModel();
                    }) ?? new UserViewModel();
                }
            }
            catch (Exception)
            {
                //WriteLog("HealthGauge.Web.Models.UserModel - GetAllUsersList", Ex.Message);
            }
            return model;
        }

        public async Task<bool> UpdatepasswordforUser(int? userid, string password)
        {
            bool retresult = false;
            try
            {
                var entity = await Task.Run(() =>
                {
                    return (from u in _Db.Users where (u.Id == userid) select new { u }).FirstOrDefault();
                });

                if (entity != null)
                {
                    entity.u.Password = await _Utility.Encrypt(password);
                    _Db.Users.Update(entity.u);
                    await _Db.SaveChangesAsync();
                    retresult = true;
                }
            }
            catch (Exception)
            {
                // WriteLog("HealthGauge.Web.Models.UserModel - UpdatepasswordforUser", Ex.Message);
            }
            return retresult;
        }

        public async Task<UserViewModel> GetUsersDetails(Int32 ID)
        {
            UserViewModel model = new UserViewModel();

            try
            {
                if (ID > 0)
                {
                    model = await Task.Run(() =>
                    {
                        return (from u in _Db.Users
                                where !u.IsDeleted && u.Id == ID
                                select new UserViewModel
                                {
                                    Id = u.Id,
                                    Firstname = u.Firstname,
                                    Lastname = u.Lastname,
                                    Email = u.Email,
                                    Username = u.Username,
                                }).FirstOrDefault();
                    });
                }
            }
            catch (Exception Ex)
            {
                //WriteLog("HealthGauge.Web.Models.UserModel - GetAllUsersList", Ex.Message);
            }
            return model;
        }
        public async Task<Boolean> CheckPassword(int? userid, string password)
        {
            Boolean retresult = false;
            try
            {
                string encryptedPassword = await _Utility.Encrypt(password);
                var entity = (from u in _Db.Users
                              where (u.Id == userid && u.Password == encryptedPassword)
                              select new { u }).FirstOrDefault();
                if (entity != null)
                {
                    retresult = true;
                }
            }
            catch (Exception Ex)
            {
                // WriteLog("HealthGauge.Web.Models.UserModel - UpdatepasswordforUser", Ex.Message);
            }
            return retresult;
        }


    }
}
