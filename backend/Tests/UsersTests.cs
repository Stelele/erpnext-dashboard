using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Application.Requests;

namespace Tests;

public class UsersTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestFactory _factory;

    public UsersTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateUser_Returns201()
    {
        await ResetAsync();
        var request = new CreateUserRequest("Test User", "test@example.com", []);

        var response = await _client.PostAsJsonAsync("/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = await response.ReadCreatedIdAsync();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_Returns400()
    {
        await ResetAsync();
        var request = new CreateUserRequest("Bad User", "not-an-email", []);

        var response = await _client.PostAsJsonAsync("/users", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsEmptyList()
    {
        await ResetAsync();
        var response = await _client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetUserById_Returns404_WhenNotFound()
    {
        await ResetAsync();
        var response = await _client.GetAsync($"/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ReturnsUserWithCompanies_WhenExists()
    {
        await ResetAsync();
        var userId = await CreateUserAsync("John Doe", "john@example.com");

        var response = await _client.GetAsync($"/users/{userId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("john@example.com", user.Email);
        Assert.NotNull(user.Companies);
    }

    [Fact]
    public async Task DeleteUser_Returns204()
    {
        await ResetAsync();
        var userId = await CreateUserAsync("To Delete", "delete@example.com");

        var response = await _client.DeleteAsync($"/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/users/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<Guid> CreateUserAsync(string name, string email)
    {
        var request = new CreateUserRequest(name, email, []);
        var response = await _client.PostAsJsonAsync("/users", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }
}
