using Infrastructure.Persistence.Contexts;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.AcceptanceTests.TestHost;

namespace Web.Api.AcceptanceTests;

public class TaxonIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public void SeededTaxonomy_IsAvailableInDb()
    {
        // seed taxonomy via scoped DbContext
        factory.Seed(db =>
        {
            var tax = Core.Catalog.Taxonomies.Taxonomy.Create(name: "default", storeId: Guid.NewGuid()).Value;
            db.Taxonomies.Add(tax);
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var found = db.Taxonomies.FirstOrDefault();
        found.ShouldNotBeNull();
        found.Name.ShouldBe("default");
    }
}
