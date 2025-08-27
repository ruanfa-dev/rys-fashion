using Hangfire.Dashboard;

namespace Infrastructure.BackgroundServices.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access only to authenticated users with admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}