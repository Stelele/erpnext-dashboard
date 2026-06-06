# User-Company Association Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add POST and DELETE endpoints to manage user-company associations via the many-to-many relationship.

**Architecture:** Two new MediatR commands with handlers, wired to RESTful endpoints following existing CQRS patterns.

**Tech Stack:** .NET 10, EF Core, MediatR, Minimal APIs

---

### Task 1: Create AddUserToCompanyCommand

**Files:**
- Create: `backend/Application/Users/AddUserToCompanyCommand.cs`

- [ ] **Step 1: Write the command record and validator**

```csharp
using Application.Abstractions;
using FluentValidation;

namespace Application.Users;

public record AddUserToCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;

public sealed class AddUserToCompanyCommandValidator : AbstractValidator<AddUserToCompanyCommand>
{
    public AddUserToCompanyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/Application/Users/AddUserToCompanyCommand.cs
git commit -m "feat: add AddUserToCompanyCommand record and validator"
```

### Task 2: Create AddUserToCompanyCommandHandler

**Files:**
- Create: `backend/Application/Users/AddUserToCompanyCommandHandler.cs`
- Modify: `backend/Domain/Exceptions/Exceptions.cs`

- [ ] **Step 1: Add NotFoundException to exceptions**

```csharp
namespace Domain.Exceptions;

public class DuplicateDomainMemberException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
```

- [ ] **Step 2: Write the handler**

```csharp
using Application.Abstractions;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class AddUserToCompanyCommandHandler(DashboardDbContext db) : ICommandHandler<AddUserToCompanyCommand>
{
    public async Task Handle(AddUserToCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([request.UserId], cancellationToken: cancellationToken);
        if (user is null)
            throw new NotFoundException($"User with ID {request.UserId} not found.");

        var company = await db.Companies.FindAsync([request.CompanyId], cancellationToken: cancellationToken);
        if (company is null)
            throw new NotFoundException($"Company with ID {request.CompanyId} not found.");

        var alreadyLinked = await db.Users
            .AnyAsync(u => u.Id == request.UserId && u.Companies.Any(c => c.Id == request.CompanyId), cancellationToken);

        if (alreadyLinked)
            throw new DuplicateDomainMemberException("User is already linked to this company.");

        user.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Update Abstractions/Command.cs to support void commands**

The existing `ICommand<TResponse>` requires a response type. We need `ICommand` (void). Check if it exists, if not add:

```csharp
using MediatR;

namespace Application.Abstractions;

internal interface ICommand : IRequest;
internal interface ICommand<TResponse> : IRequest<TResponse>;

internal interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>;

internal interface ICommandHandler<TRequest> : IRequestHandler<TRequest>
    where TRequest : ICommand;
```

- [ ] **Step 4: Commit**

```bash
git add backend/Application/Users/AddUserToCompanyCommandHandler.cs backend/Domain/Exceptions/Exceptions.cs backend/Application/Abstractions/Command.cs
git commit -m "feat: add AddUserToCompanyCommandHandler and void ICommand support"
```

### Task 3: Create RemoveUserFromCompanyCommand

**Files:**
- Create: `backend/Application/Users/RemoveUserFromCompanyCommand.cs`

- [ ] **Step 1: Write the command record and validator**

```csharp
using Application.Abstractions;
using FluentValidation;

namespace Application.Users;

public record RemoveUserFromCompanyCommand(
    Guid UserId,
    Guid CompanyId
) : ICommand;

public sealed class RemoveUserFromCompanyCommandValidator : AbstractValidator<RemoveUserFromCompanyCommand>
{
    public RemoveUserFromCompanyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/Application/Users/RemoveUserFromCompanyCommand.cs
git commit -m "feat: add RemoveUserFromCompanyCommand record and validator"
```

### Task 4: Create RemoveUserFromCompanyCommandHandler

**Files:**
- Create: `backend/Application/Users/RemoveUserFromCompanyCommandHandler.cs`

- [ ] **Step 1: Write the handler**

```csharp
using Application.Abstractions;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class RemoveUserFromCompanyCommandHandler(DashboardDbContext db) : ICommandHandler<RemoveUserFromCompanyCommand>
{
    public async Task Handle(RemoveUserFromCompanyCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException($"User with ID {request.UserId} not found.");

        var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
        if (company is null)
            throw new NotFoundException($"Link between user and company {request.CompanyId} not found.");

        user.Companies.Remove(company);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/Application/Users/RemoveUserFromCompanyCommandHandler.cs
git commit -m "feat: add RemoveUserFromCompanyCommandHandler"
```

### Task 5: Add Endpoints to UsersEndpoints.cs

**Files:**
- Modify: `backend/Endpoints/Endpoints/UsersEndpoints.cs`

- [ ] **Step 1: Add POST and DELETE routes**

Add these before `return app;` in `MapUsersEndpoints`:

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add backend/Endpoints/Endpoints/UsersEndpoints.cs
git commit -m "feat: add AddUserToCompany and RemoveUserFromCompany endpoints"
```

### Task 6: Build and Verify

- [ ] **Step 1: Build the solution**

```bash
dotnet build
```

Expected: BUILD SUCCEEDED with no errors

- [ ] **Step 2: Commit if build succeeds**

```bash
git commit --allow-empty -m "build: verify solution builds successfully"
```
