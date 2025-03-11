using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nova.DB;
using System.Threading.Tasks;

namespace Nova.Web.Controllers
{
    public class AccountsController : Controller
    {
        NovaDBContext _db;
        public AccountsController(NovaDBContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Login()
        {
            var data = await _db.Roles.ToListAsync();
            return View();
        }
    }
}
