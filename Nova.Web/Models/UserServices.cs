using Microsoft.EntityFrameworkCore;
using Nova.DB;
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
                         }).FirstOrDefaultAsync() ?? new UserViewModel();

            if (UVM != null && UVM.Id > 0)
            {
                SetUserDataToSession(UVM);
                return UVM;
            }

            return new UserViewModel();
        }

        public async Task<UserViewModel> CheckEmailExists(string EmailID)
        {
            UserViewModel UVM = new UserViewModel();
            try
            {
                UVM = await Task.Run(() =>
                {
                    return (from u in _Db.Users
                            where u.Email.Trim().ToLower() == EmailID.Trim().ToLower() && u.IsActive && !u.IsDeleted
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

        public async Task<UserViewModel> GetUsersDetailsByID(int UserID)
        {
            UserViewModel model = new UserViewModel();

            try
            {
                if (UserID > 0)
                {
                    model = await (from u in _Db.Users
                                   where u.IsActive && !u.IsDeleted && u.Id == UserID
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Firstname = u.Firstname,
                                       Lastname = u.Lastname,
                                       Email = u.Email,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync() ?? new UserViewModel();
                }
            }
            catch (Exception ex)
            {

            }
            return model;
        }

        private void SetUserDataToSession(UserViewModel model)
        {
            try
            {
                _Utility.SetSessionValue("LoggedInUserID", model.Id);
                _Utility.SetSessionValue("LoggedInUserName", model.Username);
                _Utility.SetSessionValue("LoggedInFirstName", model.Firstname);
                _Utility.SetSessionValue("LoggedInLastName", model.Lastname);
                _Utility.SetSessionValue("LoggedInEmail", model.Email);
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
                model.Email = _Utility.GetSessionValue("LoggedInEmail").ToString() ?? "";
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
                _List = await Task.Run(() =>
                {
                    return (from u in _Db.Users
                            where u.IsActive && !u.IsDeleted
                            orderby u.Id descending
                            select new UserViewModel
                            {
                                Id = u.Id,
                                Firstname = u.Firstname,
                                Lastname = u.Lastname,
                                Email = u.Email,
                                Username = u.Username,
                                CreatedDate = u.CreatedDate,
                            }).ToList();
                });
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
                                   where u.IsActive && !u.IsDeleted && u.Id == UserID
                                   select new UserViewModel
                                   {
                                       Id = u.Id,
                                       Firstname = u.Firstname,
                                       Lastname = u.Lastname,
                                       Email = u.Email,
                                       Username = u.Username
                                   }).FirstOrDefaultAsync() ?? new UserViewModel();
                }
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            return model;
        }
        public async Task<bool> CheckDuplicateEmail(string EmailID, int userid)
        {
            bool result = false;

            try
            {
                var UVM = await Task.Run(() =>
                {
                    return (from u in _Db.Users
                            where u.Email.ToLower() == EmailID.Trim().ToLower() && !u.IsDeleted && u.Id != userid
                            select new UserViewModel
                            {
                                Username = u.Username
                            }).FirstOrDefault();
                });

                if (UVM != null)
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

        public async Task<bool> CheckDuplicateUsername(string userName, int userid)
        {
            bool result = false;
            try
            {
                var UVM = await Task.Run(() =>
                {
                    return (from u in _Db.Users
                            where u.Username.ToLower() == userName.Trim().ToLower() && !u.IsDeleted && u.Id != userid
                            select new UserViewModel
                            {
                                Username = u.Username
                            }).FirstOrDefault();
                });

                if (UVM != null)
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

                    if (entity != null && !string.IsNullOrEmpty(entity.Firstname))
                    {
                        entity.Username = model.Username;
                        entity.Firstname = model.Firstname;
                        entity.Lastname = model.Lastname;
                        entity.Email = model.Email;
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
    }
}
