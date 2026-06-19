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
        private IPasswordHasherService _passwordHasher;
        private ILogger<UserServices> _logger;
        public UserServices(NovaDBContext Db, IUtilityServices Utility, IPasswordHasherService passwordHasher, ILogger<UserServices> logger)
        {
            _Db = Db;
            _Utility = Utility;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }
        public async Task<UserViewModel> CheckLogin(UserViewModel model)
        {
            // Fetch by username only — we can't filter on password in SQL because each
            // stored hash has its own salt. Verify the hash in code instead.
            var user = await _Db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.IsActive &&
                    !u.IsDeleted &&
                    u.Username == model.Username);

            if (user == null)
            {
                return new UserViewModel();
            }

            bool isValid = _passwordHasher.VerifyPassword(user.Password, model.Password, out bool needsRehash);
            if (!isValid)
            {
                return new UserViewModel();
            }

            // If the stored hash used older parameters, transparently upgrade it on
            // successful login. The user never notices.
            if (needsRehash)
            {
                user.Password = _passwordHasher.HashPassword(model.Password);
                await _Db.SaveChangesAsync();
            }

            return new UserViewModel
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                Firstname = user.Firstname ?? string.Empty,
                Lastname = user.Lastname ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RoleId = user.RoleId,
                Rolename = user.Role.Rolename
                // NOTE: do NOT echo the password back into the view model.
            };
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if email exists for EmailID {EmailID}.", EmailID);
            }
            return UVM;
        }

        public async Task<bool> SaveGuid(string guid, int userId)
        {
            try
            {
                var user = await _Db.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    return false;
                }

                user.ResetPasswordToken = guid;
                // Give the reset link a real, bounded lifetime (1 hour).
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);

                await _Db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save reset token for user {UserId}.", userId);
                return false;
            }
        }


        public async Task<UserViewModel> GetUserDetailByGUID(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return new UserViewModel();
            }

            // Enforce expiry: a token is only valid if it hasn't expired.
            var model = await (from u in _Db.Users
                               where !u.IsDeleted
                                     && u.ResetPasswordToken == guid
                                     && u.ResetPasswordTokenExpiry != null
                                     && u.ResetPasswordTokenExpiry >= DateTime.UtcNow
                               select new UserViewModel
                               {
                                   Id = u.Id,
                                   UserId = u.Id,
                                   Firstname = u.Firstname ?? string.Empty,
                                   Lastname = u.Lastname ?? string.Empty,
                                   Email = u.Email ?? string.Empty,
                                   Username = u.Username ?? string.Empty,
                                   RoleId = u.RoleId,
                                   Rolename = u.Role.Rolename,
                                   Phone = u.Phone
                               }).FirstOrDefaultAsync();

            return model ?? new UserViewModel();
        }


        public async Task<bool> UpdatePasswordForUser(int userId, string newPassword)
        {
            try
            {
                var user = await _Db.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted);

                if (user == null)
                {
                    return false;
                }

                user.Password = _passwordHasher.HashPassword(newPassword);

                // Invalidate the reset token once the password has been changed,
                // so the same reset link can't be reused.
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _Db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update password for user {UserId}.", userId);
                return false;
            }
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
                _logger.LogError(ex, "Failed to set user data to session for user {UserId}.", model.Id);
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


        public async Task<bool> CheckPassword(int? userId, string password)
        {
            if (userId is null or <= 0)
            {
                return false;
            }

            var user = await _Db.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            return user != null && _passwordHasher.VerifyPassword(user.Password, password, out _);
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
                                   TwoFactorCodeExpiry = u.TwoFactorCodeExpiry,
                                   Phone = u.Phone
                               }).ToListAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all users list.");
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
                                       TwoFactorCodeExpiry = u.TwoFactorCodeExpiry,
                                       Phone = u.Phone
                                   }).FirstOrDefaultAsync() ?? new UserViewModel();

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user details for user ID {UserID}.", UserID);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check duplicate email for EmailID {EmailID} and UserID {UserID}.", EmailID, userid);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check duplicate username for Username {Username} and UserID {UserID}.", userName, userid);
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
                        if (!string.IsNullOrEmpty(model.Phone) && !string.IsNullOrWhiteSpace(model.Phone))
                        {
                            entity.Phone = model.Phone;
                            entity.ModifiedDate = DateTime.Now;
                            entity.ModifiedBy = GetUserDataFromSession().Id;
                        }
                        if (!string.IsNullOrEmpty(model.TwoFactorCode) && !string.IsNullOrWhiteSpace(model.TwoFactorCode))
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user with ID {UserID}.", model.Id);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID {UserID}.", UserID);
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
                _logger.LogError(ex, "Failed to log out user.");
            }
        }

        public async Task<bool> Generate2FACode(int id)
        {
            try
            {
                var user = await _Db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive && !u.IsDeleted);
                if (user == null)
                {
                    return false;
                }

                // Cryptographically-strong 6-digit code (Random is not suitable for security tokens).
                string code = System.Security.Cryptography.RandomNumberGenerator
                    .GetInt32(100000, 1000000).ToString();

                user.TwoFactorCode = code;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
                await _Db.SaveChangesAsync();

                var tokens = new Dictionary<string, string>
                {
                    ["Pseudo"] = user.Firstname,
                    ["2FACode"] = code
                };

                await _Utility.SendEmailAsync("Nova Asset Management - 2FA Code", user.Email, "2FA.html", tokens);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate 2FA code for user {UserId}.", id);
                return false;
            }
        }


        public async Task<bool> Check2FACode(int id, string twoFactorCode)
        {
            if (string.IsNullOrWhiteSpace(twoFactorCode))
            {
                return false;
            }

            var user = await _Db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive && !u.IsDeleted);

            if (user == null
                || user.TwoFactorCode != twoFactorCode
                || user.TwoFactorCodeExpiry == null
                || user.TwoFactorCodeExpiry < DateTime.UtcNow)
            {
                return false;
            }

            // Single-use: clear the code once consumed.
            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            await _Db.SaveChangesAsync();

            SetUserDataToSession(new UserViewModel
            {
                Id = user.Id,
                Username = user.Username ?? string.Empty,
                Firstname = user.Firstname ?? string.Empty,
                Lastname = user.Lastname ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RoleId = user.RoleId,
                Rolename = user.Role.Rolename
            });

            return true;
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
            try
            {
                var entity = new Users
                {
                    Firstname = model.Firstname.Trim(),
                    Lastname = model.Lastname.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    Username = model.Username.Trim(),
                    Phone = model.Phone?.Trim(),
                    RoleId = model.RoleId,
                    Password = _passwordHasher.HashPassword(model.NewPassword),
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = GetUserDataFromSession().Id,
                    IsActive = true,
                    IsDeleted = false
                };

                _Db.Users.Add(entity);
                await _Db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new user {Username}.", model.Username);
                return false;
            }
        }

    }
}
