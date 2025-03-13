using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using Nova.DB.Utitlity;
using Nova.Web.Models;
using Nova.Web.Utitlity;
using System.Threading.Tasks;

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
            RemoveModelStateItem("Firstname,Lastname,Email");
            var data = await _db.Roles.ToListAsync();
            if (ModelState.IsValid)
            {
                UVM = await _Service.logins(model);
                if (model.RememberMe)
                {
                    await _Utility.SetCookies("NovaLogin", model.Username, 30);
                }
                else
                {
                    await _Utility.RemoveCookies("NovaLogin");
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
