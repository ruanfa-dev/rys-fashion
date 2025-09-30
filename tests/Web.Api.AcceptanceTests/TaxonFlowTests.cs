using System.Net.Http.Json;

using Core.Catalog.Taxonomies;

using Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Models;

using Shouldly;

using UseCases.Admin.Catalogs.Taxons.Commons;

using Web.Api.AcceptanceTests.TestHost;

namespace Web.Api.AcceptanceTests;

public class TaxonFlowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_Update_Taxon_Flow_Works_EndToEnd()
    {
        // Seed a taxonomy
        Guid taxonomyId = Guid.NewGuid();
        factory.Seed(db =>
        {
            Taxonomy tax = Core.Catalog.Taxonomies.Taxonomy.Create("seed", taxonomyId).Value;
            db.Taxonomies.Add(tax);
            db.SaveChanges();
        });

        // Create a parent taxon via API
        var parentReq = new
        {
            Name = "Parent",
            TaxonomyId = taxonomyId
        };

    HttpResponseMessage createParentResp = await _client.PostAsJsonAsync("/api/admin/taxons", parentReq, TestContext.Current.CancellationToken);
        createParentResp.EnsureSuccessStatusCode();
    ApiResponse<TaxonResult.ListItem>? parentApiResp = await createParentResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    TaxonResult.ListItem parentApi = parentApiResp!.Data!;
        parentApi.ShouldNotBeNull();
    parentApi.Id.ShouldNotBe(Guid.Empty);

        // Create child taxon with ParentId via API
        var childReq = new
        {
            Name = "Child T",
            TaxonomyId = taxonomyId,
            ParentId = parentApi.Id
        };

    HttpResponseMessage createChildResp = await _client.PostAsJsonAsync("/api/admin/taxons", childReq, TestContext.Current.CancellationToken);
        createChildResp.EnsureSuccessStatusCode();
    ApiResponse<TaxonResult.ListItem>? childApiResp = await createChildResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    TaxonResult.ListItem childApi = childApiResp!.Data!;
    childApi.ShouldNotBeNull();
    childApi.Permalink.ShouldNotBeNull();
    childApi.Permalink.ShouldContain("child-t");

        // Update child name via PUT and verify permalink changed accordingly
        var updateReq = new
        {
            Name = "Child New",
            ParentId = parentApi.Id
        };

    string updateUrl = string.Concat("/api/admin/taxons/", childApi.Id.ToString());
    HttpResponseMessage updateResp = await _client.PutAsJsonAsync(updateUrl, updateReq, TestContext.Current.CancellationToken);
        updateResp.EnsureSuccessStatusCode();
    ApiResponse<TaxonResult.ListItem>? updatedResp = await updateResp.Content.ReadFromJsonAsync<SharedKernel.Models.ApiResponse<UseCases.Admin.Catalogs.Taxons.Commons.TaxonResult.ListItem>>(TestContext.Current.CancellationToken);
    TaxonResult.ListItem updated = updatedResp!.Data!;
    updated.ShouldNotBeNull();
    updated.PrettyName.ShouldNotBeNull();
    updated.PrettyName.ShouldContain("Parent");
    updated.Permalink.ShouldNotBeNull();
    updated.Permalink.ShouldContain("child-new");
    }
}
