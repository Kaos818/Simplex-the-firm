using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SimplexLawFirm.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSessionRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is null)
        {
            var request = context.HttpContext.Request;
            var wantsJson = request.Headers.Accept.Any(x => x?.Contains("application/json") == true)
                || string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (wantsJson) { context.Result = new UnauthorizedResult(); return; }
            var returnUrl = request.Path.ToString() + request.QueryString.ToString();
            context.Result = new RedirectToActionResult("Login", "Home", new { returnUrl });
            return;
        }
        var role = context.HttpContext.Session.GetString("UserRole");
        var authorised = roles.Contains(role, StringComparer.OrdinalIgnoreCase)
            || string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase)
                && roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        if (!authorised)
            context.Result = new ForbidResult();
    }
}
