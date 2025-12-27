using Microsoft.AspNetCore.Authorization;

namespace Api.Authentication;

public class HasScopeRequirement(string scope) : IAuthorizationRequirement
{
    public string Scope { get; } = scope;
}


public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasScopeRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst("scope");

        if (scopeClaim == null)
            return Task.CompletedTask;

        var scopes = scopeClaim.Value.Split(' ');

        if (scopes.Contains(requirement.Scope))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
