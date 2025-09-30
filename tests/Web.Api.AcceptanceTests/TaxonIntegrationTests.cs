using Core.Catalog.Taxonomies;

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
            Taxonomy tax = Core.Catalog.Taxonomies.Taxonomy.Create(name: "default", storeId: Guid.NewGuid()).Value;
            db.Taxonomies.Add(tax);
        });

        using IServiceScope scope = factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Taxonomy? found = db.Taxonomies.FirstOrDefault();
        found.ShouldNotBeNull();
        found.Name.ShouldBe("default");
    }
}
