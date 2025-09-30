using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Contexts;

namespace Web.Api.AcceptanceTests.TestHost;

public static class TestDbHelpers
{
    public static void Seed(this CustomWebApplicationFactory factory, Action<ApplicationDbContext> seedAction)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        seedAction(db);
        db.SaveChanges();
    }
}
