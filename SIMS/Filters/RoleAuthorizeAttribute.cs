using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RoleAuthorizeAttribute : ActionFilterAttribute
{
    private readonly int _roleId;
    public RoleAuthorizeAttribute(int roleId) { _roleId = roleId; }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var role = filterContext.HttpContext.Session.GetString("RID");

        if (string.IsNullOrEmpty(role) || int.Parse(role) != _roleId)
        {
            filterContext.Result = new RedirectResult("/Home/Index");
        }
    }
}