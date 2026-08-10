using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SimplexLawFirm.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSessionUserAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Session.GetInt32("UserId") is not null) return;
        var request = context.HttpContext.Request;
        var wantsJson = request.Headers.Accept.Any(x => x?.Contains("application/json") == true)
            || string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        context.Result = wantsJson ? new UnauthorizedResult() : new RedirectToActionResult("Login", "Home", null);
    }
}
