using Hangfire.Dashboard;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.BackgroundServices.Filters;

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        HttpContext? httpContext = context.GetHttpContext();

        // Allow access only to authenticated users with admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}