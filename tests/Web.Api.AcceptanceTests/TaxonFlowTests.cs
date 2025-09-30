using System.Net.Http.Json;
using Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Web.Api.AcceptanceTests.TestHost;

namespace Web.Api.AcceptanceTests;

public class TaxonFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_Update_Taxon_Flow_Works_EndToEnd()
    {
        // Seed a taxonomy
        var taxonomyId = Guid.NewGuid();
        factory.Seed(db =>
        {
            var tax = Core.Catalog.Taxonomies.Taxonomy.Create("seed", taxonomyId).Value;
            db.Taxonomies.Add(tax);
            db.SaveChanges();
        });

        // Create a parent taxon via API
        var parentReq = new
        {
            Name = "Parent",
            TaxonomyId = taxonomyId
        };

    var createParentResp = await _client.PostAsJsonAsync("/api/admin/taxons", parentReq, TestContext.Current.CancellationToken);
        createParentResp.EnsureSuccessStatusCode();
    var parentApiResp = await createParentResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    var parentApi = parentApiResp!.Data!;
        parentApi.ShouldNotBeNull();
    parentApi.Id.ShouldNotBe(Guid.Empty);

        // Create child taxon with ParentId via API
        var childReq = new
        {
            Name = "Child T",
            TaxonomyId = taxonomyId,
            ParentId = parentApi.Id
        };

    var createChildResp = await _client.PostAsJsonAsync("/api/admin/taxons", childReq, TestContext.Current.CancellationToken);
        createChildResp.EnsureSuccessStatusCode();
    var childApiResp = await createChildResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    var childApi = childApiResp!.Data!;
    childApi.ShouldNotBeNull();
    childApi.Permalink.ShouldNotBeNull();
    childApi.Permalink.ShouldContain("child-t");

        // Update child name via PUT and verify permalink changed accordingly
        var updateReq = new
        {
            Name = "Child New",
            ParentId = parentApi.Id
        };

    var updateUrl = string.Concat("/api/admin/taxons/", childApi.Id.ToString());
    var updateResp = await _client.PutAsJsonAsync(updateUrl, updateReq, TestContext.Current.CancellationToken);
        updateResp.EnsureSuccessStatusCode();
    var updatedResp = await updateResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    var updated = updatedResp!.Data!;
    updated.ShouldNotBeNull();
    updated.PrettyName.ShouldNotBeNull();
    updated.PrettyName.ShouldContain("Parent");
    updated.Permalink.ShouldNotBeNull();
    updated.Permalink.ShouldContain("child-new");
    }
}
