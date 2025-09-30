using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Persistence.Contexts;

namespace Web.Api.AcceptanceTests.TestHost;

public static class TestDbHelpers
{
    public static void Seed(this CustomWebApplicationFactory factory, Action<ApplicationDbContext> seedAction)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        seedAction(db);
        db.SaveChanges();
    }
}
