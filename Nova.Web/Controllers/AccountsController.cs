using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.DB.Utitlity;
using Nova.Web.Models;
using Nova.Web.Utitlity;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Nova.Web.Controllers
{
    public class AccountsController : Controller
    {
        NovaDBContext _db;
        private IUserService _Service;
        private IUtilityService _Utility;
        public AccountsController(NovaDBContext db, IUserService Ser, IUtilityService Uti)
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
            if (Request.Cookies["NovaLogin"] != null)
            {
                model.RememberMe = true;
            }
            else
            {
                model.Email = String.Empty;
                model.RememberMe = false;
            }
            return View();
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login([FromForm] UserViewModel model)
        {
            UserViewModel UVM = new UserViewModel();
            RemoveModelStateItem("Firstname,Lastname,Email,NewPassword,ConfirmPassword");
            var data = await _db.Roles.ToListAsync();
            if (ModelState.IsValid)
            {
                UVM = await _Service.logins(model);
                if (UVM != null && UVM.Id > 0)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, UVM.Username),
                new Claim(ClaimTypes.NameIdentifier, UVM.Id.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    if (model.RememberMe)
                    {
                        await _Utility.SetCookies("NovaLogin", model.Username, 30);
                        await _Utility.SetCookies("NovaLogin", model.Password, 30);
                    }
                    else
                    {
                        await _Utility.RemoveCookies("NovaLogin");
                    }

                    return Json(new { url = Url.Action("Index", "Home") });
                }


            }
            return Json(new { url = Url.Action("Index", "Home") });

        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await _Utility.SetSessionValue("LoggedInUserID", 0);
            await _Utility.SetSessionValue("LoggedInUserName", String.Empty);
            // HttpContext.Session.Clear();
            await ClearCookies();   // CLEAR COOKIES AFTER LOGOUT

            return RedirectToAction("LogIn", "Accounts");
        }
        public async Task ClearCookies()
        {
            await _Utility.RemoveCookies("LoggedInUserID");
            await _Utility.RemoveCookies("LoggedInUserName");
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword([FromForm] UserViewModel model)
        {
            UserViewModel lvm = new UserViewModel();
            if (!string.IsNullOrEmpty(model.Email.ToString()))
            {
                lvm = await _Service.CheckEmailIDExit(model.Email.ToString());
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
                    var SendmailResult = Task.Run(() => _Utility.SendEmailAsync("Reset Password Requested", model.Email, "ForgotPassword.html", lvm.Firstname, objDict));
                    if (SendmailResult.Result)
                    {
                        TempData["SuccessMessage"] = "A One Time Password (OTP) has been sent to your registered email. Please check your email.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Mail sending fail";
                    }

                    bool Saveguidornot = await _Service.SaveGuid(guid.ToString(), lvm.Id);
                    TempData["IsShowVerification"] = "true";
                    ViewBag.SuccessMessage = "A One Time Password (OTP) has been sent to your registered email. Please check your email.";
                    return Json(new { url = Url.Action("Login", "Accounts") });
                }
                else
                {
                    TempData["SuccessMessage"] = "Email does not exists";
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
        public async Task<IActionResult> ResetPasswords(UserViewModel model)
        {
            RemoveModelStateItem("Firstname,Lastname,Email,Username,NewPassword,Password");
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                if (await _Service.UpdatepasswordforUser(model.UserId.ToString(), model.ConfirmPassword))
                {
                    Dictionary<string, string> objDict = new Dictionary<string, string>();
                    objDict.Add("Pseudo", model.Firstname);
                    var SendmailResult = Task.Run(() => _Utility.SendEmailAsync("Your Password has Changed Successfully. If you don't do that plase comtact admin", model.Email, "ChangePassword.html", model.Firstname, objDict));
                    // MH.SendEmail("Password Changed Successfully", model.emailid, "ChangePassword.html", objDict);
                    if (SendmailResult.Result)
                    {
                        TempData["SuccessMessage"] = "Your Password has successfully changed";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Your Password has not changed";
                    }


                    return Json(new { url = Url.Action("Login", "Accounts") });
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
