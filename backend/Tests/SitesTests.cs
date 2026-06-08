using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;

namespace Tests;

public class SitesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public SitesTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateSite_Returns201()
    {
        await ResetAsync();
        var request = new CreateSiteRequest("Test Site", "https://test.example.com", "A test site", "token123");

        var response = await _client.PostAsJsonAsync("/sites", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task GetSites_ReturnsEmptyList()
    {
        await ResetAsync();
        var response = await _client.GetAsync("/sites");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sites = await response.Content.ReadFromJsonAsync<List<SiteResponse>>();
        Assert.NotNull(sites);
        Assert.Empty(sites);
    }

    [Fact]
    public async Task GetSiteById_Returns404_WhenNotFound()
    {
        await ResetAsync();
        var response = await _client.GetAsync($"/sites/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSiteById_ReturnsSite_WhenExists()
    {
        await ResetAsync();
        var siteId = await CreateSiteAsync("Site One", "https://one.example.com");

        var response = await _client.GetAsync($"/sites/{siteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var site = await response.Content.ReadFromJsonAsync<SiteResponse>();
        Assert.NotNull(site);
        Assert.Equal("Site One", site.Name);
        Assert.Equal("https://one.example.com", site.Url);
    }

    [Fact]
    public async Task DeleteSite_Returns204()
    {
        await ResetAsync();
        var siteId = await CreateSiteAsync("To Delete", "https://delete.example.com");

        var response = await _client.DeleteAsync($"/sites/{siteId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/sites/{siteId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSite_WithInvalidUrl_Returns400()
    {
        await ResetAsync();
        var request = new CreateSiteRequest("Bad Site", "not-a-url", "", "token");

        var response = await _client.PostAsJsonAsync("/sites", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateSiteAsync(string name, string url)
    {
        var request = new CreateSiteRequest(name, url, "", "token");
        var response = await _client.PostAsJsonAsync("/sites", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }
}
