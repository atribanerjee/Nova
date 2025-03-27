using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.DB.POCO;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;

namespace Nova.Web.Models
{
    public class UserServices : IUserServices
    {
        public NovaDBContext _Db;
        private IUtilityServices _Utility;
        public UserServices(NovaDBContext Db, IUtilityServices Utility)
        {
            _Db = Db;
            _Utility = Utility;
        }
        public async Task<UserViewModel> CheckLogin(UserViewModel model)
        {
            UserViewModel UVM = new UserViewModel();

            string password = await _Utility.Encrypt(model.Password);
            UVM = await (from u in _Db.Users
                         where u.IsActive && !u.IsDeleted && u.Username == model.Username && u.Password == password
                         select new UserViewModel
                         {
                             Id = u.Id,
                             Username = u.Username ?? string.Empty,
                             Firstname = u.Firstname ?? string.Empty,
                             Lastname = u.Lastname ?? string.Empty,
                             Email = u.Email ?? string.Empty,
                             RoleId = u.RoleId,
                             Password = model.Password,
                             Rolename = u.Role.Rolename
                         }).FirstOrDefaultAsync() ?? new UserViewModel();

            if (UVM != null && UVM.Id > 0)
            {
                return UVM;
            }

            return new UserViewModel();
        }

        public async Task<UserViewModel> CheckEmailExists(string EmailID)
        {
            UserViewModel UVM = new UserViewModel();
            try
            {
                UVM = await (from u in _Db.Users
                             where u.Email.Trim().ToLower() == EmailID.Trim().ToLower() && u.IsActive && !u.IsDeleted
                             select new UserViewModel
                             {
                                 Username = u.Username,
                                 Firstname = u.Firstname,
                                 Lastname = u.Lastname,
                                 Id = u.Id
                             }).FirstOrDefaultAsync() ?? new UserViewModel();

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

        public async Task<bool> SaveGuid(string guid, int UserID)
        {
            bool Result = false;
            var entity = await _Db.Users.Where(x => x.Id == UserID && x.IsActive && !x.IsDeleted).FirstOrDefaultAsync();

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
                    model = await (from u in _Db.Users
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
                                   }).FirstOrDefaultAsync() ?? new UserViewModel();

                }
            }
            catch (Exception)
            {

            }
            return model;
        }

        public async Task<bool> UpdatePasswordForUser(int UserID, string Password)
        {
            bool retresult = false;
            try
            {
                var entity = await _Db.Users.Where(x => x.Id == UserID && x.IsActive && !x.IsDeleted).FirstOrDefaultAsync();

                if (entity != null)
                {
                    entity.Password = await _Utility.Encrypt(Password);

                    _Db.Users.Update(entity);
                    await _Db.SaveChangesAsync();
                    retresult = true;
                }
            }
            catch (Exception ex)
            {

            }
            return retresult;
        }

        private void SetUserDataToSession(UserViewModel model)
        {
            try
            {
                _Utility.SetSessionValue("LoggedInUserID", model.Id);
                _Utility.SetSessionValue("LoggedInUserName", model.Username.Trim());
                _Utility.SetSessionValue("LoggedInFirstName", model.Firstname.Trim());
                _Utility.SetSessionValue("LoggedInLastName", model.Lastname.Trim());
                _Utility.SetSessionValue("LoggedInFullName", string.Concat(model.Firstname.Trim(), " ", model.Lastname));
                _Utility.SetSessionValue("LoggedInEmail", model.Email.ToLower().Trim());
                _Utility.SetSessionValue("LoggedInRoleID", model.RoleId);
                _Utility.SetSessionValue("LoggedInRolename", model.Rolename.ToLower().Trim());
            }
            catch (Exception ex)
            {

            }
        }

        public UserViewModel GetUserDataFromSession()
        {
            UserViewModel model = new UserViewModel();
            try
            {
                model.Id = Convert.ToInt32(_Utility.GetSessionValue("LoggedInUserID"));
                model.Username = _Utility.GetSessionValue("LoggedInUserName").ToString() ?? "";
                model.Firstname = _Utility.GetSessionValue("LoggedInFirstName").ToString() ?? "";
                model.Lastname = _Utility.GetSessionValue("LoggedInLastName").ToString() ?? "";
                model.Fullname = _Utility.GetSessionValue("LoggedInFullName").ToString() ?? "";
                model.Email = _Utility.GetSessionValue("LoggedInEmail").ToString() ?? "";
                model.RoleId = Convert.ToInt32(_Utility.GetSessionValue("LoggedInRoleID"));
                model.Rolename = _Utility.GetSessionValue("LoggedInRolename").ToString() ?? "";
            }
            catch (Exception ex)
            {

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
        public async Task<List<UserViewModel>> GetAllUsersList(UserViewModel UVM)
        {
            List<UserViewModel> _List = new List<UserViewModel>();

            try
            {
                _List = await (from u in _Db.Users
                               where !u.IsDeleted
                               orderby u.Id descending
                               select new UserViewModel
                               {
                                   Id = u.Id,
                                   Firstname = u.Firstname,
                                   Lastname = u.Lastname,
                                   Fullname = string.Concat(u.Firstname, " ", u.Lastname),
                                   Email = u.Email,
                                   Username = u.Username,
                                   CreatedDate = u.CreatedDate,
                                   IsActive = u.IsActive,
                                   IsDeleted = u.IsDeleted,
                                   RoleId = u.RoleId,
                                   Rolename = u.Role.Rolename,
                                   ResetPasswordToken = u.ResetPasswordToken,
                                   ResetPasswordTokenExpiry = u.ResetPasswordTokenExpiry,
                                   TwoFactorCode = u.TwoFactorCode,
                                   TwoFactorCodeExpiry = u.TwoFactorCodeExpiry
                               }).ToListAsync();

            }
            catch (Exception Ex)
            {
                // Handle exception
            }

            return _List;
        }

        public async Task<UserViewModel> GetUserDetailByUserID(int UserID)
        {
            UserViewModel model = new UserViewModel();
            try
            {
                if (UserID > 0)
                {
                    model = await (from u in _Db.Users
                                   where u.Id == UserID && !u.IsDeleted
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Firstname = u.Firstname,
                                       Lastname = u.Lastname,
                                       Fullname = string.Concat(u.Firstname, " ", u.Lastname),
                                       Email = u.Email,
                                       Username = u.Username,
                                       CreatedDate = u.CreatedDate,
                                       IsActive = u.IsActive,
                                       IsDeleted = u.IsDeleted,
                                       RoleId = u.RoleId,
                                       Rolename = u.Role.Rolename,
                                       ResetPasswordToken = u.ResetPasswordToken,
                                       ResetPasswordTokenExpiry = u.ResetPasswordTokenExpiry,
                                       TwoFactorCode = u.TwoFactorCode,
                                       TwoFactorCodeExpiry = u.TwoFactorCodeExpiry
                                   }).FirstOrDefaultAsync() ?? new UserViewModel();

                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            return model;
        }
        public async Task<bool> CheckDuplicateEmail(string EmailID, int? userid)
        {
            bool result = false;
            var model = new UserViewModel();
            try
            {
                if (userid != null && userid > 0)
                {
                    model = await (from u in _Db.Users
                                   where u.Email.ToLower().Trim() == EmailID.Trim().ToLower() && !u.IsDeleted && u.Id != userid
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync();
                }
                else
                {
                    model = await (from u in _Db.Users
                                   where u.Email.ToLower().Trim() == EmailID.Trim().ToLower() && !u.IsDeleted
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync();
                }

                if (model != null && model.Id > 0)
                {
                    result = true;
                }
            }
            catch (Exception Ex)
            {

            }
            return result;
        }

        public async Task<bool> CheckDuplicateUsername(string userName, int? userid)
        {
            bool result = false;
            var model = new UserViewModel();
            try
            {
                if (userid != null && userid > 0)
                {
                    model = await (from u in _Db.Users
                                   where u.Username.ToLower() == userName.Trim().ToLower() && !u.IsDeleted && u.Id != userid
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync();
                }
                else
                {
                    model = await (from u in _Db.Users
                                   where u.Username.ToLower() == userName.Trim().ToLower() && !u.IsDeleted
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync();
                }

                if (model != null && model.Id > 0)
                {
                    result = true;
                }
            }
            catch (Exception Ex)
            {
                // Handle exception
            }
            return result;
        }
        public async Task<Int32> UpdateUser(UserViewModel model)
        {
            Int32 userid = 0;

            try
            {
                if (!String.IsNullOrEmpty(model.ToString()))
                {
                    var entity = await _Db.Users.Where(x => x.Id == model.Id && !x.IsDeleted).FirstOrDefaultAsync();
                    if (entity != null)
                    {
                        if (!string.IsNullOrEmpty(model.Firstname) && !string.IsNullOrWhiteSpace(model.Firstname) && entity.Firstname.Trim().ToLower() != model.Firstname.Trim().ToLower())
                        {
                            entity.Firstname = model.Firstname;
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedBy = GetUserDataFromSession().Id;
                        }
                        if (!string.IsNullOrEmpty(model.Lastname) && !string.IsNullOrWhiteSpace(model.Lastname) && entity.Lastname.Trim().ToLower() != model.Lastname.Trim().ToLower())
                        {
                            entity.Lastname = model.Lastname;
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedBy = GetUserDataFromSession().Id;
                        }
                        if (entity.RoleId != model.RoleId)
                        {
                            entity.RoleId = model.RoleId;
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedBy = GetUserDataFromSession().Id;
                        }
                        if (!string.IsNullOrEmpty(model.TwoFactorCode) && !string.IsNullOrWhiteSpace(model.TwoFactorCode) && entity.TwoFactorCode.Trim().ToLower() != model.TwoFactorCode.Trim().ToLower())
                        {
                            entity.TwoFactorCode = model.TwoFactorCode;
                            entity.TwoFactorCodeExpiry = model.TwoFactorCodeExpiry;
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedBy = GetUserDataFromSession().Id;
                        }



                        _Db.Users.Update(entity);
                        await _Db.SaveChangesAsync();
                        userid = entity.Id;
                    }

                }
            }
            catch (Exception Ex)
            {
                // Handle exception
            }
            return userid;
        }

        public async Task<bool> DeleteUserByUserID(int UserID)
        {
            bool Result = false;
            try
            {
                var Entity = await _Db.Users.FindAsync(UserID);
                if (Entity != null && Entity.Id > 0)
                {
                    Entity.IsDeleted = true;
                    _Db.Users.Update(Entity);
                    await _Db.SaveChangesAsync();
                    Result = true;
                }
            }
            catch (Exception Ex)
            {
                // WriteLog("HealthGauge.Web.Models.UserModel - DeleteUserByUserID", Ex.Message);
            }
            return Result;
        }
        public void LogOut()
        {
            try
            {
                _Utility.LogOut();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task<bool> Generate2FACode(int id)
        {
            bool Result = false;
            try
            {
                var UserDetails = await GetUserDetailByUserID(id);
                if (UserDetails != null)
                {
                    UserDetails.TwoFactorCode = new Random().Next(100000, 999999).ToString();
                    UserDetails.TwoFactorCodeExpiry = DateTime.Now.AddMinutes(5);

                    await UpdateUser(UserDetails);

                    Dictionary<string, string> objDict = new Dictionary<string, string>();
                    objDict.Add("Pseudo", UserDetails.Firstname);
                    objDict.Add("2FACode", UserDetails.TwoFactorCode);

                    await _Utility.SendEmailAsync("Nova Asset Management - 2FA Code", UserDetails.Email, "2FA.html", objDict);

                    Result = true;
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            return Result;
        }

        public async Task<bool> Check2FACode(int id, string twoFactorCode)
        {
            bool Result = false;
            try
            {
                var UserDetails = await GetUserDetailByUserID(id);
                if (UserDetails != null && !string.IsNullOrEmpty(twoFactorCode) && !string.IsNullOrWhiteSpace(twoFactorCode))
                {
                    if (UserDetails.TwoFactorCode == twoFactorCode && UserDetails.TwoFactorCodeExpiry >= DateTime.Now)
                    {
                        SetUserDataToSession(UserDetails);
                        Result = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return Result;
        }

        public async Task<bool> StatusUpdateForUserByUserID(int userId, bool status)
        {
            bool Result = false;
            try
            {
                var Entity = await _Db.Users.FindAsync(userId);
                if (Entity != null && Entity.Id > 0)
                {
                    Entity.IsActive = status;
                    _Db.Users.Update(Entity);

                    await _Db.SaveChangesAsync();
                    Result = true;
                }
            }
            catch (Exception ex)
            {

            }
            return Result;
        }

        public async Task<bool> SaveUser(UserViewModel model)
        {
            bool Result = false;
            try
            {
                var entity = new Users();

                entity.Firstname = model.Firstname.Trim();
                entity.Lastname = model.Lastname.Trim();
                entity.Email = model.Email.Trim().ToLower();
                entity.Username = model.Username.Trim();
                entity.RoleId = model.RoleId;
                entity.Password = model.NewPassword;
                entity.CreatedDate = DateTime.Now;
                entity.CreatedBy = GetUserDataFromSession().Id;
                entity.IsActive = true;
                entity.IsDeleted = false;

                _Db.Users.Add(entity);
                await _Db.SaveChangesAsync();
                Result = true;
            }
            catch (Exception ex)
            {

            }
            return Result;
        }
    }
}
