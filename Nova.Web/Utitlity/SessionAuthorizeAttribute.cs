using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nova.Web.Interfaces;
using Nova.Web.Models;
using Nova.Web.Utitlity;

public class SessionAuthorizeAttribute : TypeFilterAttribute
{
    public SessionAuthorizeAttribute(string role) : base(typeof(SessionAuthorizeFilter))
    {
        Arguments = new object[] { role };
    }
}

public class SessionAuthorizeFilter : IAuthorizationFilter
{
    private readonly string _role;
    private readonly IUserServices _userServices;

    public SessionAuthorizeFilter(string role, IUserServices userServices)
    {
        if (!string.IsNullOrEmpty(role) && !string.IsNullOrWhiteSpace(role))
        {
            _role = role;
        }
        _userServices = userServices;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = _userServices.GetUserDataFromSession().Id;
        var userRole = _userServices.GetUserDataFromSession().Rolename;

        if (!string.IsNullOrEmpty(_role) && !string.IsNullOrWhiteSpace(_role))
        {
            if(userRole == null || userRole.ToString() != _role)
            {
                context.Result = new RedirectToActionResult("Login", "Accounts", null);
            }
        }

        if (userId == null || userId == 0)
        {
            context.Result = new RedirectToActionResult("Login", "Accounts", null);
        }
    }
}
