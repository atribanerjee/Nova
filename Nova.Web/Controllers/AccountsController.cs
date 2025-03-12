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
                    Set("NovaLogin", model.Username, 30);
                }
                else
                {
                    Remove("NovaLogin");
                }


            }
          
           return RedirectToAction("Index", "Home");
           
        }

        public void ClearCookies()
        {
            Remove("LoggedInUserID");
            Remove("LoggedInUserName");
        }
        public string Get(string key)
        {
            return Request.Cookies[key];
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void Set(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddDays(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);
            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void Remove(string key)
        {
            Response.Cookies.Delete(key);
        }

        public ActionResult LogOut()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _Utility.SetSessionValue("LoggedInUserID", 0);
            _Utility.SetSessionValue("LoggedInUserName", String.Empty);
            // HttpContext.Session.Clear();
            ClearCookies();   // CLEAR COOKIES AFTER LOGOUT

            return RedirectToAction("LogIn", "Accounts");
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
