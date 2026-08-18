using System.Security.Claims;
using System.Text.Encodings.Web;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Authentication;

public class SessionAuthenticationHandler(
    IOptionsMonitor<SessionAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DashboardDbContext db)
    : AuthenticationHandler<SessionAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = BearerToken.Extract(Request.Headers.Authorization.ToString());
        if (token is null)
            return AuthenticateResult.NoResult();

        var session = await db.Sessions
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == OpaqueToken.Hash(token));

        if (session is null || session.User is null)
            return AuthenticateResult.Fail("Invalid session token.");

        var now = DateTimeOffset.UtcNow;
        await db.Sessions
            .Where(s => s.Id == session.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastUsedOn, now));

        var claims = new[]
        {
            new Claim("user_id", session.User.Id.ToString()),
            new Claim(ClaimTypes.Name, session.User.Name),
            new Claim(ClaimTypes.Email, session.User.Email),
            new Claim("role", session.User.Role.ToString()),
            new Claim("scope", string.Join(' ', RolePermissions.For(session.User.Role))),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}