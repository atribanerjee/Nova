using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Nova.Web.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AccountsController : Controller
    {
        NovaDBContext _db;
        private IUserServices _Service;
        private IUtilityServices _Utility;
        public AccountsController(NovaDBContext db, IUserServices Ser, IUtilityServices Uti)
        {
            _db = db;
            _Service = Ser;
            _Utility = Uti;
        }
        [HttpGet]
        public async Task<IActionResult> Login()
        {
            UserViewModel model = new UserViewModel();
            var data = await _db.Roles.ToListAsync();
            if (TempData["SuccessMessage"] != null)
            {
                if (!String.IsNullOrEmpty(Convert.ToString(TempData["SuccessMessage"])))
                {
                    ViewBag.SuccessMessage = Convert.ToString(TempData["SuccessMessage"]);
                }
            }
            if (TempData["ErrorMessage"] != null)
            {
                if (!String.IsNullOrEmpty(Convert.ToString(TempData["ErrorMessage"])))
                {
                    ViewBag.ErrorMessage = Convert.ToString(TempData["ErrorMessage"]);
                }
            }
            
            if (_Utility.GetCookies("NovaLogin") != null)
            {
                model.RememberMe = true;
                model.Username = await _Utility.GetCookies("NovaLogin");
            }
            else
            {
                model.Email = String.Empty;
                model.RememberMe = false;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]        
        public async Task<IActionResult> Login([FromForm] UserViewModel model)
        {
            UserViewModel UVM = new UserViewModel();
            RemoveModelStateItem("Firstname,Lastname,Email,NewPassword,ConfirmPassword");
            var data = await _db.Roles.ToListAsync();
            if (ModelState.IsValid)
            {
                UVM = await _Service.CheckLogin(model);
                if (UVM != null && UVM.Id > 0)
                {
                   
                    if (model.RememberMe)
                    {
                        await _Utility.SetCookies("NovaLogin", model.Username, 30);

                    }
                    else
                    {
                        await _Utility.RemoveCookies("NovaLogin");
                    }

                    return Json(new { url = Url.Action("UserList", "Accounts") });
                }
                else
                {

                    TempData["ErrorMessage"] = "Invalid username or password";
                    return Json(new { url = Url.Action("Login", "Accounts") });

                }


            }
            TempData["ErrorMessage"] = "Model state is invalid.";
            return Json(new { url = Url.Action("Login", "Accounts") });

        }

        public IActionResult LogOut()
        {
            _Service.LogOut();

            return RedirectToAction("LogIn", "Accounts");
        }
        public async Task ClearCookies()
        {
            await _Utility.RemoveCookies("LoggedInUserID");
            await _Utility.RemoveCookies("LoggedInUserName");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetPassword([FromForm] UserViewModel model)
        {
            UserViewModel lvm = new UserViewModel();
            if (!string.IsNullOrEmpty(model.Email.ToString()))
            {
                lvm = await _Service.CheckEmailExists(model.Email.ToString());
                if (lvm != null)
                {
                    Guid guid = Guid.NewGuid();

                    Dictionary<string, string> objDict = new Dictionary<string, string>();
                    objDict.Add("Pseudo", lvm.Firstname);
                    objDict.Add("Year", DateTime.Now.Year.ToString());

                    Microsoft.Extensions.Configuration.IConfiguration _Configuration = new ConfigurationBuilder()
                        .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();

                    objDict.Add("ActivationUrl", _Configuration["EmailSettings:BaseURl"] + "Accounts/ResetPasswords/" + guid);
                    var SendmailResult = Task.Run(() => _Utility.SendEmailAsync("Notification: Reset Password Requested", model.Email, "ForgotPassword.html", lvm.Firstname, objDict));
                    if (SendmailResult.Result)
                    {
                        TempData["SuccessMessage"] = "An email has been sent to your registered email. Please check your email.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Email sending is failed.";
                    }

                    bool Saveguidornot = await _Service.SaveGuid(guid.ToString(), lvm.Id);
                    TempData["IsShowVerification"] = "true";
                    return Json(new { url = Url.Action("Login", "Accounts") });
                }
                else
                {
                    TempData["SuccessMessage"] = "Invalid Email address.";
                }
            }
            return Json(new { url = Url.Action("Login", "Accounts") });


        }

        [HttpGet]
        public async Task<IActionResult> ResetPasswords(string ID)//guid
        {
            // UserModel um = new UserModel();
            UserViewModel uvm = new UserViewModel();
            uvm = await _Service.GetUserDetailByGUID(ID);
            if (!string.IsNullOrEmpty(uvm.ToString()))
            {
                return View(uvm);
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid Url.";
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswords(UserViewModel model)
        {
            RemoveModelStateItem("Firstname,Lastname,Email,Username,NewPassword,Password");
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                if (await _Service.UpdatePasswordForUser(model.UserId, model.ConfirmPassword))
                {
                    Dictionary<string, string> objDict = new Dictionary<string, string>();
                    objDict.Add("Pseudo", model.Firstname);
                    var SendmailResult = Task.Run(() => _Utility.SendEmailAsync("Notification: Password Updated Successfully", model.Email, "ChangePassword.html", model.Firstname, objDict));

                    if (SendmailResult.Result)
                    {
                        TempData["SuccessMessage"] = "Your Password has successfully changed";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Your Password has not changed";
                    }


                    return RedirectToAction("Login", "Accounts");
                }
                else
                {
                    ViewBag.ErrorMessage = "Reset Password failed";
                    return View(model);
                }
            }
            else
            {
                return View(model);
            }


        }


        public IActionResult ResetPassword()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetPassword(string password, string NewPassword, string ConfirmPassword)
        {
            bool Result = false;
            RemoveModelStateItem("firstname,lastname,username,emailid");
            if (ModelState.IsValid)
            {
                if (NewPassword == ConfirmPassword)
                {
                    UserViewModel model = _Service.GetUserDataFromSession();


                    // Update the following line in the ResetPassword method

                    Result = await _Service.UpdatePasswordForUser(model.UserId, NewPassword);
                    if (Result)
                    {
                        Dictionary<string, string> objDict = new Dictionary<string, string>();
                        objDict.Add("Pseudo", model.Firstname);
                        var SendmailResult = Task.Run(() => _Utility.SendEmailAsync("Notification: Password Updated Successfully", model.Email, "ChangePassword.html", model.Firstname, objDict));

                        if (SendmailResult.Result)
                        {
                            TempData["SuccessMessage"] = "Your Password has successfully changed";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Your Password has not changed";
                        }
                        LogOut();
                        return Json(new { Result = true, Message = "Your Password has successfully changed" });
                    }
                    else
                    {
                        return Json(new { Result = true, Message = "New and old password should not be same" });
                    }
                }
                else
                {
                    return Json(new { Result = false, Message = "Passwords are not matching." });
                }

            }
            return Json(new { Result = true, Message = "Missing data." });
        }
        
        [HttpGet]
        public async Task<JsonResult> Checkpassword(String Password)
        {
            // string? uid = _Utility.GetSessionValue("LoggedInUserID").ToString();
            int? uid = HttpContext.Session.GetInt32("LoggedInUserID");
            if (uid != null && await _Service.CheckPassword(uid, Password))
            {
                return Json(new { Result = true, Message = "New and old password same" });
            }
            else
            {
                return Json(new { Result = false, Message = "" });
            }
        }

        [HttpGet]
        [SessionAuthorize("")]        
        public async Task<IActionResult> UserList(int PageNumber, int PageSize, string SearchValue = "", string SortBy = "desc")
        {
            UserViewModel UVM = new UserViewModel();
            UVM.IsActive = true;
            //UVM.PageNumber = PageNumber;
            //UVM.PageSize = PageSize;
            //UVM.SearchValue = SearchValue;
            //UVM.SortByValue = SortBy;
            if (!String.IsNullOrEmpty(SearchValue))
            {
                ViewBag.SearchValue = SearchValue;
            }
            return View("UserList",await _Service.GetAllUsersList(UVM));
        }

        [HttpGet]
        [SessionAuthorize("")]        
        public async Task<IActionResult> Edit(int ID)
        {
                UserViewModel uvm = new UserViewModel();

                uvm =await _Service.GetUserDetailByUserID(ID);

                //uvm.UserRoleList = UM.GetRoleList();

             //   ViewBag.LoggedInUserRoleID = UM.GetLoggedInUserInfo().UserRoleID;

                return View(uvm);
           
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("")]        
        public async Task<IActionResult> Edit([FromForm] UserViewModel model)
        {
            int? uid = 0;

            RemoveModelStateItem("password,NewPassword,ConfirmPassword,forgotpasswordguid");

            if (ModelState.IsValid)
            {
                var userid = _Service.GetUserDataFromSession().Id;
               // model = _Service.GetUsersDetails(ID); 
                Int32 id = 0;
                bool checkduplicateemai =await _Service.CheckDuplicateEmail(model.Email, model.Id);
                bool checkduplicateusername = await _Service.CheckDuplicateUsername(model.Username, model.Id);
                if (!checkduplicateemai && !checkduplicateusername)
                {
                    id =await _Service.UpdateUser(model);
                    return RedirectToAction("UserList", "Accounts");
                }
                else
                {
                    if (checkduplicateemai)
                    {
                        ModelState.AddModelError("Email", "Email already exists.");
                    }
                    if (checkduplicateusername)
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                    }
                    return View("Edit", model);
                }
            }
            else
            {
                return View(model);
            }
           
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [SessionAuthorize("")]        
        public async Task<JsonResult> DeleteUser(int UserId)
        {
            if (await _Service.DeleteUserByUserID(UserId))
            {
                return Json(new { Result = true, Message = "User deleted successfully." });
            }
            else
            {
                return Json(new { Result = false, Message = "User deleted unsuccessful." });
            }

        }

        private void RemoveModelStateItem(String data)
        {
            try
            {
                String[] items = data.Split(',');
                foreach (String item in items)
                {
                    ModelState.Remove(item);
                }
            }
            catch { }
        }
    }
}
