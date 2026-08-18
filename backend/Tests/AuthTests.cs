using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Auth;
using Application.Requests;
using Domain.Users;
using Infrastructure.Email;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class AuthTests : IClassFixture<SessionAuthTestFactory>
{
    private readonly SessionAuthTestFactory _factory;

    public AuthTests(SessionAuthTestFactory factory) => _factory = factory;

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    private async Task<Guid> SeedUserAsync(string email, string password, Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = User.Create("Test User", email, role);
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<HttpClient> LoginAsync(string email = "admin@test.com", string password = "TestPass123!")
    {
        await SeedUserAsync(email, password, Role.Admin);
        return await _factory.CreateAuthenticatedClientAsync(email, password);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "TestPass123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"role\":\"admin\"", body);
        var result = JsonSerializer.Deserialize<LoginResponse>(body, TestJson.Options);
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("admin@test.com", result.User.Email);
        Assert.Equal(Role.Admin, result.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LocksAccountAfterFiveFailures()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);
        var client = _factory.CreateClientWithoutAuth();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "TestPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task Login_WithLockedAccount_Returns401()
    {
        await ResetAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = User.Create("Locked", "locked@test.com", Role.Admin);
            user.PasswordHash = hasher.HashPassword(user, "TestPass123!");
            user.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(15);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "locked@test.com", password = "TestPass123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithValidSessionToken_Succeeds()
    {
        await ResetAsync();
        var client = await LoginAsync();
        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithGarbageToken_Returns401()
    {
        await ResetAsync();
        var client = _factory.CreateClientWithoutAuth();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");
        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesSession()
    {
        await ResetAsync();
        var client = await LoginAsync();

        var logout = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ViewerToken_CannotCallWriteEndpoint()
    {
        await ResetAsync();
        await SeedUserAsync("viewer@test.com", "TestPass123!", Role.Viewer);
        var client = await _factory.CreateAuthenticatedClientAsync("viewer@test.com", "TestPass123!");

        var read = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var write = await client.PostAsync(
            "/users",
            JsonContent.Create(new CreateUserRequest("New", "new@test.com", Role.Viewer, []), mediaType: null, TestJson.Options));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanCallWriteEndpoint()
    {
        await ResetAsync();
        var client = await LoginAsync();
        var response = await client.PostAsync(
            "/users",
            JsonContent.Create(new CreateUserRequest("New", "new@test.com", Role.Viewer, []), mediaType: null, TestJson.Options));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_SendsEmail_WithTokenLink()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        Assert.NotNull(sender);
        var email = Assert.Single(sender!.Sent);
        Assert.Equal("admin@test.com", email.To);
        Assert.Contains("/reset-password?token=", email.HtmlBody);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_StillReturns200()
    {
        await ResetAsync();
        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "nobody@test.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        Assert.NotNull(sender);
        Assert.Empty(sender!.Sent);
    }

    [Fact]
    public async Task ResetPassword_SetsNewPassword_AndRevokesOldSessions()
    {
        await ResetAsync();
        var oldClient = await LoginAsync();

        var req = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        req.EnsureSuccessStatusCode();

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        var token = sender!.Sent.Single().HtmlBody.Split("token=")[1].Split('"')[0];

        var client = _factory.CreateClientWithoutAuth();
        var reset = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var oldGet = await oldClient.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, oldGet.StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Viewer_WithNoCompanies_CannotSeeSites()
    {
        await ResetAsync();
        var admin = await LoginAsync();

        var create = await admin.PostAsJsonAsync("/sites",
            new CreateSiteRequest("Secret Site", "https://secret.example.com", "", "tok"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        await SeedUserAsync("viewer@test.com", "TestPass123!", Role.Viewer);
        var viewer = await _factory.CreateAuthenticatedClientAsync("viewer@test.com", "TestPass123!");

        var response = await viewer.GetAsync("/sites");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sites = await response.Content.ReadFromJsonAsync<List<Application.DTOs.SiteResponse>>();
        Assert.NotNull(sites);
        Assert.Empty(sites);
    }

    [Fact]
    public async Task Viewer_WithNoCompanies_CannotSeeSiteById()
    {
        await ResetAsync();
        var admin = await LoginAsync();

        var create = await admin.PostAsJsonAsync("/sites",
            new CreateSiteRequest("Secret Site", "https://secret.example.com", "", "tok"));
        var siteId = await create.ReadCreatedIdAsync();

        await SeedUserAsync("viewer@test.com", "TestPass123!", Role.Viewer);
        var viewer = await _factory.CreateAuthenticatedClientAsync("viewer@test.com", "TestPass123!");

        var response = await viewer.GetAsync($"/sites/{siteId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_WithNoCompanies_CannotSeeCompanies()
    {
        await ResetAsync();
        var admin = await LoginAsync();

        var createSite = await admin.PostAsJsonAsync("/sites",
            new CreateSiteRequest("Site", "https://site.example.com", "", "tok"));
        var siteId = await createSite.ReadCreatedIdAsync();
        var createCompany = await admin.PostAsJsonAsync("/companies",
            new Application.Requests.CreateCompanyRequest(siteId, "Secret Co", ""));
        Assert.Equal(HttpStatusCode.Created, createCompany.StatusCode);

        await SeedUserAsync("viewer@test.com", "TestPass123!", Role.Viewer);
        var viewer = await _factory.CreateAuthenticatedClientAsync("viewer@test.com", "TestPass123!");

        var response = await viewer.GetAsync("/companies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<Application.DTOs.CompanyResponse>>();
        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task Admin_WithNoCompanies_StillSeesAllSites()
    {
        await ResetAsync();
        var admin = await LoginAsync();

        var create = await admin.PostAsJsonAsync("/sites",
            new CreateSiteRequest("Site", "https://site.example.com", "", "tok"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var response = await admin.GetAsync("/sites");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sites = await response.Content.ReadFromJsonAsync<List<Application.DTOs.SiteResponse>>();
        Assert.NotNull(sites);
        Assert.Single(sites);
    }

    [Fact]
    public async Task ResetPassword_WithUsedToken_Fails()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var req = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        req.EnsureSuccessStatusCode();

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        var token = sender!.Sent.Single().HtmlBody.Split("token=")[1].Split('"')[0];

        var client = _factory.CreateClientWithoutAuth();
        var first = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "Another123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }
}
