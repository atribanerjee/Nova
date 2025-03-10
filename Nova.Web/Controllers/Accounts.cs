using Microsoft.AspNetCore.Mvc;

namespace Nova.Web.Controllers
{
    public class Accounts : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}
