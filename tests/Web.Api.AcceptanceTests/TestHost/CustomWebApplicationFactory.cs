using Infrastructure.Persistence.Contexts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Web.Api.TestHost;

namespace Web.Api.AcceptanceTests.TestHost;

public class CustomWebApplicationFactory : WebApplicationFactory<TestHostMarker>, IAsyncDisposable
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Run the app in Development mode for tests so storage options validation uses local defaults
        // and persistence configuration still uses an in-memory provider per project setup.
        builder.UseEnvironment("Development");

        IHost host = base.CreateHost(builder);

        // Ensure the in-memory database is created after the host has been built
        using IServiceScope scope = host.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
