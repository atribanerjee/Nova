using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nova.DB;
using Nova.Web.Interfaces;
using Nova.Web.Utitlity;
using Nova.Web.ViewModels;
using System.Buffers;

namespace Nova.Web.Controllers
{
    public class UserRoleController : Controller
    {
        NovaDBContext _db;
        private IUserServices _Service;
        private IUtilityServices _Utility;
        private IuserRoleService _UserRole;
        public UserRoleController(NovaDBContext db, IUserServices Ser, IUtilityServices Uti, IuserRoleService userRole)
        {
            _db = db;
            _Service = Ser;
            _Utility = Uti;
            _UserRole = userRole;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> RoleLIst()
        {
            try
            {
                string? uid = string.Empty;
                UserRoleViewModel model = new UserRoleViewModel();
                if (_Service.GetUserDataFromSession().Id.ToString() != null)
                {
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
                   
                    uid = _Service.GetUserDataFromSession().Id.ToString();
                 
                    model.PageNumber = 0;
                    model.PageSize = 10;
                    return View(await _UserRole.GetAllRoleList(model, ""));
                }
                else
                {
                    uid = "1";
                   
                    model.PageNumber = 0;
                    model.PageSize = 10;
                    return View(await _UserRole.GetAllRoleList(model, ""));
                }
            }
            catch (Exception ex)
            {


            }

            return View();

        }
        [HttpPost]
        public async Task<IActionResult> GetRoleLIst(string ID)
        {

            UserRoleViewModel model = new UserRoleViewModel();
            List<UserRoleViewModel> _List = await _UserRole.GetAllRoleList(model, "");
            //   return View(_List);


            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Request.Form["start"].FirstOrDefault();
            var length = Request.Form["length"].FirstOrDefault();
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var searchvalue = Request.Form["search[value]"].FirstOrDefault();
            model.PageSize = length != null ? Convert.ToInt32(length) : 0;

            int pageSize = model.PageSize;
            int skip = start != null ? Convert.ToInt32(start) : 0;
            model.PageNumber = skip;
            _List = await _UserRole.GetAllRoleList(model, searchvalue);
            int? totalRecords = _List[0].TotalRecords;


            var jsonData = new
            {
                draw=draw,
                recordsFiltered = totalRecords,
                recordsTotal = totalRecords,
                data = _List
            };
            return new JsonResult(jsonData);
        }


    }
}
