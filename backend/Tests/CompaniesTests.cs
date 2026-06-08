using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;

namespace Tests;

public class CompaniesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public CompaniesTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateCompany_Returns201()
    {
        await ResetAsync();
        var siteId = await CreateSiteAsync();
        var request = new CreateCompanyRequest(siteId, "Test Company", "A test company");

        var response = await _client.PostAsJsonAsync("/companies", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task GetCompanies_ReturnsEmptyList()
    {
        await ResetAsync();
        var response = await _client.GetAsync("/companies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task GetCompanyById_Returns404_WhenNotFound()
    {
        await ResetAsync();
        var response = await _client.GetAsync($"/companies/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanyById_ReturnsCompanyWithSiteId_WhenExists()
    {
        await ResetAsync();
        var siteId = await CreateSiteAsync();
        var companyRequest = new CreateCompanyRequest(siteId, "My Company", "");
        var companyResponse = await _client.PostAsJsonAsync("/companies", companyRequest);
        var companyId = await companyResponse.ReadCreatedIdAsync();

        var response = await _client.GetAsync($"/companies/{companyId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
        Assert.NotNull(company);
        Assert.Equal("My Company", company.Name);
        Assert.Equal(siteId, company.SiteId);
    }

    [Fact]
    public async Task DeleteCompany_Returns204()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync("To Delete", "Delete Site");

        var response = await _client.DeleteAsync($"/companies/{companyId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/companies/{companyId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<Guid> CreateSiteAsync()
    {
        var request = new CreateSiteRequest($"Site-{Guid.NewGuid():N}", $"https://{Guid.NewGuid():N}.example.com", "", "token");
        var response = await _client.PostAsJsonAsync("/sites", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }

    private async Task<Guid> CreateCompanyAsync(string name, string siteName)
    {
        var siteRequest = new CreateSiteRequest(siteName, $"https://{Guid.NewGuid():N}.example.com", "", "token");
        var siteResponse = await _client.PostAsJsonAsync("/sites", siteRequest);
        siteResponse.EnsureSuccessStatusCode();
        var siteId = await siteResponse.ReadCreatedIdAsync();

        var request = new CreateCompanyRequest(siteId, name, "");
        var response = await _client.PostAsJsonAsync("/companies", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }
}
