using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;
using Domain.CompanySettings;

namespace Tests;

public class ExpenseTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public ExpenseTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GetExpenseTypes_ReturnsSeededData()
    {
        await ResetAsync();
        var response = await _client.GetAsync("/api/expense-types");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var types = await response.Content.ReadFromJsonAsync<List<ExpenseTypeResponse>>();
        Assert.NotNull(types);
        Assert.NotEmpty(types);
    }

    [Fact]
    public async Task GetExpenseTypeById_ReturnsExpenseType_WhenExists()
    {
        await ResetAsync();
        var types = await GetExpenseTypesAsync();
        var firstType = types.First();

        var response = await _client.GetAsync($"/api/expense-types/{firstType.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var type = await response.Content.ReadFromJsonAsync<ExpenseTypeResponse>();
        Assert.NotNull(type);
        Assert.Equal(firstType.Id, type.Id);
    }

    [Fact]
    public async Task GetExpenseTypeById_Returns404_WhenNotFound()
    {
        await ResetAsync();
        var response = await _client.GetAsync($"/api/expense-types/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateExpenseType_Returns201()
    {
        await ResetAsync();
        var request = new CreateExpenseTypeRequest("New Expense Type", "A new type");

        var response = await _client.PostAsJsonAsync("/api/expense-types", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CreateExpenseType_WithDuplicateName_Returns409()
    {
        await ResetAsync();
        var types = await GetExpenseTypesAsync();
        var existingName = types.First().Name;

        var request = new CreateExpenseTypeRequest(existingName, "Duplicate");

        var response = await _client.PostAsJsonAsync("/api/expense-types", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExpenseType_Returns204()
    {
        await ResetAsync();
        var types = await GetExpenseTypesAsync();
        var type = types.First(t => t.Name != "Utilities");
        var request = new UpdateExpenseTypeRequest("Updated Name", "Updated description");

        var response = await _client.PutAsJsonAsync($"/api/expense-types/{type.Id}", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/expense-types/{type.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<ExpenseTypeResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task UpdateExpenseType_WithNonExistentId_Returns404()
    {
        await ResetAsync();
        var request = new UpdateExpenseTypeRequest("Name", "Desc");

        var response = await _client.PutAsJsonAsync($"/api/expense-types/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteExpenseType_Returns204()
    {
        await ResetAsync();
        var createRequest = new CreateExpenseTypeRequest("To Delete", "Will be deleted");
        var createResponse = await _client.PostAsJsonAsync("/api/expense-types", createRequest);
        var id = await createResponse.ReadCreatedIdAsync();

        var response = await _client.DeleteAsync($"/api/expense-types/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanySettings_Returns404_WhenNoCompany()
    {
        await ResetAsync();
        var response = await _client.GetAsync($"/api/companies/{Guid.NewGuid()}/settings");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAndGetCompanySettings()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync();

        var updateRequest = new UpdateCompanySettingsRequest(
            "Custom Income Account",
            PrimaryColor.Blue,
            NeutralColor.Stone
        );
        var updateResponse = await _client.PutAsJsonAsync($"/api/companies/{companyId}/settings", updateRequest);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/settings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var settings = await getResponse.Content.ReadFromJsonAsync<CompanySettingsResponse>();
        Assert.NotNull(settings);
        Assert.Equal("Custom Income Account", settings.DefaultIncomeAccountName);
        Assert.Equal(PrimaryColor.Blue, settings.PrimaryColor);
        Assert.Equal(NeutralColor.Stone, settings.NeutralColor);
    }

    [Fact]
    public async Task UpsertCompanyExpenseMappings()
    {
        await ResetAsync();
        var companyId = await CreateCompanyAsync();
        var types = await GetExpenseTypesAsync();
        var type = types.First();

        var request = new UpsertCompanyExpenseMappingsRequest(
            Mappings: [new MappingItemRequest(type.Id, "Account-123")]);

        var response = await _client.PutAsJsonAsync($"/api/companies/{companyId}/expense-mappings", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/companies/{companyId}/expense-mappings");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var mappings = await getResponse.Content.ReadFromJsonAsync<List<CompanyExpenseMappingResponse>>();
        Assert.NotNull(mappings);
        Assert.Single(mappings);
        Assert.Equal(type.Id, mappings[0].ExpenseTypeId);
        Assert.Equal("Account-123", mappings[0].ErpnextAccountName);
    }

    private async Task<List<ExpenseTypeResponse>> GetExpenseTypesAsync()
    {
        var response = await _client.GetAsync("/api/expense-types");
        return await response.Content.ReadFromJsonAsync<List<ExpenseTypeResponse>>() ?? [];
    }

    private async Task<Guid> CreateCompanyAsync()
    {
        var siteRequest = new CreateSiteRequest($"Site-{Guid.NewGuid():N}", $"https://{Guid.NewGuid():N}.example.com", "", "token");
        var siteResponse = await _client.PostAsJsonAsync("/sites", siteRequest);
        siteResponse.EnsureSuccessStatusCode();
        var siteId = await siteResponse.ReadCreatedIdAsync();

        var companyRequest = new CreateCompanyRequest(siteId, $"Company-{Guid.NewGuid():N}", "");
        var companyResponse = await _client.PostAsJsonAsync("/companies", companyRequest);
        companyResponse.EnsureSuccessStatusCode();
        return await companyResponse.ReadCreatedIdAsync();
    }
}
