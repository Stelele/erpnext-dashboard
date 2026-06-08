# DTO Structure Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Flatten response DTO inheritance hierarchy and introduce separate request DTOs at the API layer to eliminate command-as-request-body duplication.

**Architecture:** Response DTOs become single flat records per entity with nested objects included. Request DTOs live in `Api/Requests/` and contain only body fields — endpoints construct commands from route params + request body.

**Tech Stack:** C# 14, ASP.NET Core Minimal APIs, MediatR, FluentValidation, Entity Framework Core

---

### Task 1: Create Request DTOs

**Files:**
- Create: `backend/Api/Requests/CreateCompanyRequest.cs`
- Create: `backend/Api/Requests/CreateSiteRequest.cs`
- Create: `backend/Api/Requests/CreateUserRequest.cs`
- Create: `backend/Api/Requests/CreateExpenseTypeRequest.cs`
- Create: `backend/Api/Requests/UpdateExpenseTypeRequest.cs`
- Create: `backend/Api/Requests/UpsertCompanyExpenseMappingsRequest.cs`
- Create: `backend/Api/Requests/UpdateCompanySettingsRequest.cs`

- [ ] **Step 1: Create all 7 request DTO files**

`backend/Api/Requests/CreateCompanyRequest.cs`:
```csharp
using FluentValidation;

namespace Api.Requests;

public record CreateCompanyRequest(
    Guid SiteId,
    string Name,
    string Description
);

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
```

`backend/Api/Requests/CreateSiteRequest.cs`:
```csharp
using FluentValidation;

namespace Api.Requests;

public record CreateSiteRequest(
    string Name,
    string Url,
    string Description,
    string ApiToken
);

public class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().Must(BeAValidUrl).WithMessage("The URL must be a valid absolute URL.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }

    private static bool BeAValidUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && uriResult.Scheme == Uri.UriSchemeHttps;
}
```

`backend/Api/Requests/CreateUserRequest.cs`:
```csharp
using FluentValidation;

namespace Api.Requests;

public record CreateUserRequest(
    string Name,
    string Email,
    List<Guid> Companies
);

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

`backend/Api/Requests/CreateExpenseTypeRequest.cs`:
```csharp
using FluentValidation;

namespace Api.Requests;

public record CreateExpenseTypeRequest(
    string Name,
    string Description
);

public class CreateExpenseTypeRequestValidator : AbstractValidator<CreateExpenseTypeRequest>
{
    public CreateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
```

`backend/Api/Requests/UpdateExpenseTypeRequest.cs`:
```csharp
using FluentValidation;

namespace Api.Requests;

public record UpdateExpenseTypeRequest(
    string Name,
    string Description
);

public class UpdateExpenseTypeRequestValidator : AbstractValidator<UpdateExpenseTypeRequest>
{
    public UpdateExpenseTypeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(255);
    }
}
```

`backend/Api/Requests/UpsertCompanyExpenseMappingsRequest.cs`:
```csharp
namespace Api.Requests;

public record UpsertCompanyExpenseMappingsRequest(
    List<MappingItemRequest> Mappings
);

public record MappingItemRequest(Guid ExpenseTypeId, string ErpnextAccountName);
```

`backend/Api/Requests/UpdateCompanySettingsRequest.cs`:
```csharp
namespace Api.Requests;

public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName
);
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: PASS (requests are standalone, no consumers yet)

- [ ] **Step 3: Commit**

```bash
git add backend/Api/Requests/
git commit -m "feat: add API request DTOs"
```

---

### Task 2: Update Company Endpoints to Use Request DTOs

**Files:**
- Modify: `backend/Endpoints/Endpoints/CompanyEndpoints.cs`

- [ ] **Step 1: Update CompanyEndpoints.cs**

Replace the entire file content with:

```csharp
using Api.Requests;
using Application.Companies;
using Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class CompanyEndpoints
{
    public static WebApplication MapCompanyEndpoints(this WebApplication app)
    {
        app.MapGet("/companies", async (IMediator mediator, Guid[]? companyIds) =>
        {
            var query = new GetCompaniesQuery(companyIds);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithTags(Tags.Companies)
        .WithName("GetCompanies")
        .WithDescription("Retrieves a list of companies. Optionally filter by an array of company IDs.")
        .Produces<List<CompanyResponse>>(StatusCodes.Status200OK);

        app.MapGet("/companies/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var query = new GetCompanyByIdQuery(id);
            var result = await mediator.Send(query);
            return result == null ? Results.NotFound() : Results.Ok(result);
        })
        .WithTags(Tags.Companies)
        .WithName("GetCompanyById")
        .WithDescription("Retrieves a company by its unique identifier.")
        .Produces<CompanyResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/companies/{id:guid}", async (IMediator mediator, Guid id) =>
        {
            var command = new DeleteCompanyCommand(id);
            await mediator.Send(command);
            return Results.NoContent();
        })
        .WithTags(Tags.Companies)
        .WithName("DeleteCompany")
        .WithDescription("Deletes a company by its unique identifier.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/companies", async (IMediator mediator, CreateCompanyRequest request) =>
        {
            var command = new CreateCompanyCommand(request.SiteId, request.Name, request.Description);
            var result = await mediator.Send(command);
            return Results.Created($"/companies/{result}", new { id = result });
        })
        .WithTags(Tags.Companies)
        .WithName("CreateCompany")
        .WithDescription("Creates a new company with the provided details.")
        .Accepts<CreateCompanyRequest>("application/json")
        .Produces<Guid>(StatusCodes.Status201Created);

        return app;
    }
}
```

Key changes:
- `Accepts<CreateCompanyCommand>` → `Accepts<CreateCompanyRequest>`
- `.Produces<ExtendedCompanyResponse>` → `.Produces<CompanyResponse>`
- POST endpoint constructs `CreateCompanyCommand` from request body

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: FAIL — `GetCompanyByIdQueryHandler` still returns `ExtendedCompanyResponse`, which no longer matches `.Produces<CompanyResponse>`. This is expected; we fix it in Task 4.

- [ ] **Step 3: Commit**

```bash
git add backend/Endpoints/Endpoints/CompanyEndpoints.cs
git commit -m "refactor: company endpoints use request DTOs"
```

---

### Task 3: Update Sites Endpoints to Use Request DTOs

**Files:**
- Modify: `backend/Endpoints/Endpoints/SitesEndpoints.cs`

- [ ] **Step 1: Update SitesEndpoints.cs**

Replace the entire file content with:

```csharp
using Api.Requests;
using Application.DTOs;
using Application.Sites;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class SitesEndpoints
{
    public static WebApplication MapSitesEndpoints(this WebApplication app)
    {
        app.MapDelete("/sites/{id:guid}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteSiteCommand(id));
            return Results.NoContent();
        })
         .WithName("DeleteSite")
         .WithDisplayName("DeleteSite")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.DeleteSites);

        app.MapGet("/sites/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var site = await mediator.Send(new GetSiteByIdQuery(id));
            return site is not null ? Results.Ok(site) : Results.NotFound();
        })
         .WithName("GetSiteById")
         .WithDisplayName("GetSiteById")
         .Produces<SiteResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.ReadSites);

        app.MapGet("/sites", async (Guid[]? siteIds, ISender mediator) =>
        {
            var sites = await mediator.Send(new GetSitesQuery(siteIds));
            return Results.Ok(sites);
        })
         .WithName("GetAllSites")
         .WithDisplayName("GetAllSites")
         .Produces<List<SiteResponse>>(StatusCodes.Status200OK)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.ReadSites);

        app.MapPost("/sites", async (CreateSiteRequest request, ISender mediator) =>
        {
            var command = new CreateSiteCommand(request.Name, request.Url, request.Description, request.ApiToken);
            var siteId = await mediator.Send(command);
            return Results.Created($"/sites/{siteId}", new { id = siteId });
        })
         .WithName("CreateSite")
         .WithDisplayName("CreateSite")
         .Accepts<CreateSiteRequest>("application/json")
         .Produces<Guid>(StatusCodes.Status201Created)
         .WithTags(Tags.Sites)
         .RequireAuthorization(Permissions.UpdateSites);

        return app;
    }
}
```

Key changes:
- `Accepts<CreateSiteCommand>` → `Accepts<CreateSiteRequest>`
- `.Produces<ExtendedSiteResponse>` → `.Produces<SiteResponse>`
- POST endpoint constructs `CreateSiteCommand` from request body
- Fixed location URL from `/users/{siteId}` to `/sites/{siteId}`

- [ ] **Step 2: Commit**

```bash
git add backend/Endpoints/Endpoints/SitesEndpoints.cs
git commit -m "refactor: sites endpoints use request DTOs"
```

---

### Task 4: Update Users Endpoints to Use Request DTOs

**Files:**
- Modify: `backend/Endpoints/Endpoints/UsersEndpoints.cs`

- [ ] **Step 1: Update UsersEndpoints.cs**

Replace the entire file content with:

```csharp
using Api.Requests;
using Application.DTOs;
using Application.Users;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class UsersEndpoints
{
    public static WebApplication MapUsersEndpoints(this WebApplication app)
    {
        app.MapGet("/users/{id}", async (Guid id, ISender mediator) =>
        {
            var user = await mediator.Send(new GetUserByIdQuery(id));
            return user is not null ? Results.Ok(user) : Results.NotFound();
        })
         .WithName("GetUserById")
         .WithDisplayName("GetUserById")
         .Produces<UserResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.ReadUsers);

        app.MapGet("/users", async (Guid[]? ids, ISender mediator) =>
        {
            var users = await mediator.Send(new GetUsersQuery(ids));
            return Results.Ok(users);
        })
         .WithName("GetUsers")
         .Produces<List<UserResponse>>(StatusCodes.Status200OK)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.ReadUsers);

        app.MapPost("/users", async (CreateUserRequest request, ISender mediator) =>
        {
            var command = new CreateUserCommand(request.Name, request.Email, request.Companies);
            var userId = await mediator.Send(command);
            return Results.Created($"/users/{userId}", new { Id = userId });
        })
         .WithName("CreateUser")
         .WithDisplayName("CreateUser")
         .Accepts<CreateUserRequest>("application/json")
         .Produces<Guid>(StatusCodes.Status201Created)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.UpdateUsers);

        app.MapDelete("/users/{id}", async (Guid id, ISender mediator) =>
        {
            await mediator.Send(new DeleteUserCommand(id));
            return Results.NoContent();
        })
         .WithName("DeleteUser")
         .WithDisplayName("DeleteUser")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.DeleteUsers);

        app.MapPost("/users/{userId}/companies/{companyId}", async (Guid userId, Guid companyId, ISender mediator) =>
        {
            await mediator.Send(new AddUserToCompanyCommand(userId, companyId));
            return Results.NoContent();
        })
         .WithName("AddUserToCompany")
         .WithDisplayName("AddUserToCompany")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .Produces(StatusCodes.Status409Conflict)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.UpdateUsers);

        app.MapDelete("/users/{userId}/companies/{companyId}", async (Guid userId, Guid companyId, ISender mediator) =>
        {
            await mediator.Send(new RemoveUserFromCompanyCommand(userId, companyId));
            return Results.NoContent();
        })
         .WithName("RemoveUserFromCompany")
         .WithDisplayName("RemoveUserFromCompany")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.UpdateUsers);

        return app;
    }
}
```

Key changes:
- `Accepts<CreateUserCommand>` → `Accepts<CreateUserRequest>`
- `.Produces<ExtendedUserResponse>` → `.Produces<UserResponse>`
- POST endpoint constructs `CreateUserCommand` from request body
- Removed duplicate `.WithName("GetUsers")`

- [ ] **Step 2: Commit**

```bash
git add backend/Endpoints/Endpoints/UsersEndpoints.cs
git commit -m "refactor: users endpoints use request DTOs"
```

---

### Task 5: Update Expense Endpoints to Use Request DTOs

**Files:**
- Modify: `backend/Endpoints/Endpoints/ExpenseEndpoints.cs`

- [ ] **Step 1: Update ExpenseEndpoints.cs**

Replace the entire file content with:

```csharp
using Api.Requests;
using Application.CompanyExpenseMappings;
using Application.CompanySettings;
using Application.ExpenseTypes;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints;

public static class ExpenseEndpoints
{
    public static WebApplication MapExpenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags(Tags.Expenses);

        group.MapGet("/expense-types", async (IMediator mediator) =>
                await mediator.Send(new GetExpenseTypesQuery()))
            .Produces<List<ExpenseTypeResponse>>(StatusCodes.Status200OK)
            .WithName("GetExpenseTypes")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapGet("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, IMediator mediator) =>
                await mediator.Send(new GetCompanyExpenseMappingsQuery(companyId)))
            .Produces<List<CompanyExpenseMappingResponse>>(StatusCodes.Status200OK)
            .WithName("GetCompanyExpenseMappings")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, UpsertCompanyExpenseMappingsRequest request, IMediator mediator) =>
            {
                var command = new UpsertCompanyExpenseMappingsCommand(
                    companyId,
                    request.Mappings.Select(m => new MappingItem(m.ExpenseTypeId, m.ErpnextAccountName)).ToList());
                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("UpsertCompanyExpenseMappings")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapGet("/companies/{companyId:guid}/settings", async (Guid companyId, IMediator mediator) =>
            {
                var settings = await mediator.Send(new GetCompanySettingsQuery(companyId));
                return settings == null ? Results.NotFound() : Results.Ok(settings);
            })
            .Produces<CompanySettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetCompanySettings")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/companies/{companyId:guid}/settings", async (Guid companyId, UpdateCompanySettingsRequest request, IMediator mediator) =>
            {
                var command = new UpdateCompanySettingsCommand(companyId, request.DefaultIncomeAccountName);
                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("UpdateCompanySettings")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapPost("/expense-types", async (IMediator mediator, CreateExpenseTypeRequest request) =>
            {
                var command = new CreateExpenseTypeCommand(request.Name, request.Description);
                var id = await mediator.Send(command);
                return Results.Created($"/api/expense-types/{id}", new { id });
            })
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("CreateExpenseType")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapGet("/expense-types/{id:guid}", async (IMediator mediator, Guid id) =>
            {
                var result = await mediator.Send(new GetExpenseTypeByIdQuery(id));
                return result == null ? Results.NotFound() : Results.Ok(result);
            })
            .Produces<ExpenseTypeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetExpenseTypeById")
            .RequireAuthorization(Permissions.ReadExpenses);

        group.MapPut("/expense-types/{id:guid}", async (IMediator mediator, Guid id, UpdateExpenseTypeRequest request) =>
            {
                var command = new UpdateExpenseTypeCommand(id, request.Name, request.Description);
                await mediator.Send(command);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("UpdateExpenseType")
            .RequireAuthorization(Permissions.UpdateExpenses);

        group.MapDelete("/expense-types/{id:guid}", async (IMediator mediator, Guid id) =>
            {
                await mediator.Send(new DeleteExpenseTypeCommand(id));
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("DeleteExpenseType")
            .RequireAuthorization(Permissions.DeleteExpenses);

        return app;
    }
}
```

Key changes:
- All PUT/POST endpoints now bind request DTOs and construct commands — no more `if (command.Id != id) return Results.BadRequest();`
- `Accepts<CreateExpenseTypeCommand>` → `Accepts<CreateExpenseTypeRequest>`
- Removed `Application.DTOs.` prefix from `.Produces<>` calls (using directive handles it)

- [ ] **Step 2: Commit**

```bash
git add backend/Endpoints/Endpoints/ExpenseEndpoints.cs
git commit -m "refactor: expense endpoints use request DTOs"
```

---

### Task 6: Flatten Response DTOs

**Files:**
- Modify: `backend/Application/DTOs/CompanyResponse.cs`
- Modify: `backend/Application/DTOs/SiteResponse.cs`
- Modify: `backend/Application/DTOs/UserResponse.cs`

- [ ] **Step 1: Flatten CompanyResponse.cs**

Replace the entire file content with:

```csharp
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record CompanyResponse(
    Guid Id,
    string Name,
    string Description,
    SiteResponse Site
)
{
    public static CompanyResponse FromDomain(Domain.Companies.Company company) =>
        new(
            company.Id,
            company.Name,
            company.Description,
            SiteResponse.FromDomain(company.Site)
        );
}
```

- [ ] **Step 2: Flatten SiteResponse.cs**

Replace the entire file content with:

```csharp
using Domain.Sites;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record SiteResponse(
    Guid Id,
    string Name,
    string Description,
    string Url,
    string ApiToken,
    List<Guid> Companies
)
{
    public static SiteResponse FromDomain(Site site) =>
        new(
            site.Id,
            site.Name,
            site.Description,
            site.Url,
            site.ApiToken,
            [.. site.Companies.Select(c => c.Id)]
        );
}
```

- [ ] **Step 3: Flatten UserResponse.cs**

Replace the entire file content with:

```csharp
using Domain.Users;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    List<CompanyResponse> Companies
)
{
    public static UserResponse FromDomain(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            [.. user.Companies.Select(c => CompanyResponse.FromDomain(c))]
        );
}
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: FAIL — query handlers still reference `ExtendedCompanyResponse`, `ExtendedSiteResponse`, `ExtendedUserResponse`. Fix in Task 7.

- [ ] **Step 5: Commit**

```bash
git add backend/Application/DTOs/CompanyResponse.cs backend/Application/DTOs/SiteResponse.cs backend/Application/DTOs/UserResponse.cs
git commit -m "refactor: flatten response DTOs to single record per entity"
```

---

### Task 7: Update Query Handlers to Use Flattened Responses

**Files:**
- Modify: `backend/Application/Companies/GetCompanyByIdQuery.cs`
- Modify: `backend/Application/Companies/GetCompanyByIdQueryHandler.cs`
- Modify: `backend/Application/Sites/GetSiteByIdQuery.cs`
- Modify: `backend/Application/Sites/GetSiteByIdQueryHandler.cs`
- Modify: `backend/Application/Users/GetUserByIdQuery.cs`
- Modify: `backend/Application/Users/GetUserByIdQueryHandler.cs`

- [ ] **Step 1: Update GetCompanyByIdQuery.cs**

Read the current file, then update the return type from `ExtendedCompanyResponse?` to `CompanyResponse?`:

```csharp
using Application.Abstractions;
using Application.DTOs;

namespace Application.Companies;

public record GetCompanyByIdQuery(Guid Id) : IQuery<CompanyResponse?>;
```

- [ ] **Step 2: Update GetCompanyByIdQueryHandler.cs**

Replace the entire file content with:

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Companies;

public class GetCompanyByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyByIdQuery, CompanyResponse?>
{
    public async Task<CompanyResponse?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .Include(c => c.Site)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        return company == null ? null : CompanyResponse.FromDomain(company);
    }
}
```

- [ ] **Step 3: Update GetSiteByIdQuery.cs**

Read the current file, then update the return type from `ExtendedSiteResponse?` to `SiteResponse?`:

```csharp
using Application.Abstractions;
using Application.DTOs;

namespace Application.Sites;

public record GetSiteByIdQuery(Guid Id) : IQuery<SiteResponse?>;
```

- [ ] **Step 4: Update GetSiteByIdQueryHandler.cs**

Replace the entire file content with:

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Sites;

public class GetSiteByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetSiteByIdQuery, SiteResponse?>
{
    public async Task<SiteResponse?> Handle(GetSiteByIdQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites
            .Include(s => s.Companies)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        return site == null ? null : SiteResponse.FromDomain(site);
    }
}
```

- [ ] **Step 5: Update GetUserByIdQuery.cs**

Read the current file, then update the return type from `ExtendedUserResponse?` to `UserResponse?`:

```csharp
using Application.Abstractions;
using Application.DTOs;

namespace Application.Users;

public record GetUserByIdQuery(Guid Id) : IQuery<UserResponse?>;
```

- [ ] **Step 6: Update GetUserByIdQueryHandler.cs**

Replace the entire file content with:

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class GetUserByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetUserByIdQuery, UserResponse?>
{
    public async Task<UserResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Companies)
                .ThenInclude(c => c.Site)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        return user == null ? null : UserResponse.FromDomain(user);
    }
}
```

- [ ] **Step 7: Build to verify**

Run: `dotnet build`
Expected: PASS — all references to Extended/Base classes are gone.

- [ ] **Step 8: Commit**

```bash
git add backend/Application/Companies/GetCompanyByIdQuery.cs backend/Application/Companies/GetCompanyByIdQueryHandler.cs backend/Application/Sites/GetSiteByIdQuery.cs backend/Application/Sites/GetSiteByIdQueryHandler.cs backend/Application/Users/GetUserByIdQuery.cs backend/Application/Users/GetUserByIdQueryHandler.cs
git commit -m "refactor: query handlers use flattened response types"
```

---

### Task 8: Clean Up — Remove Unused Base/Extended Classes

**Files:**
- The Base/Extended classes were already removed inline in Task 6 (they lived in the same files). This task verifies no orphaned references remain.

- [ ] **Step 1: Search for any remaining references**

Run:
```bash
grep -r "BaseCompanyResponse\|ExtendedCompanyResponse\|BaseSiteResponse\|ExtendedSiteResponse\|BaseUserResponse\|ExtendedUserResponse\|SiteCompanyResponse" backend/
```
Expected: No results.

- [ ] **Step 2: Final build**

Run: `dotnet build`
Expected: PASS with zero warnings about unused types.

- [ ] **Step 3: Final commit**

```bash
git status
git add -A
git commit -m "chore: remove unused base/extended response classes"
```
