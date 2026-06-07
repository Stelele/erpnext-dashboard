# Expense Type CRUD API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add full CRUD API endpoints for managing custom expense types with validation, authorization, and soft-delete.

**Architecture:** Four new CQRS command/query files in Application layer, modifications to ExpenseType entity and ExpenseEndpoints. Follows existing patterns from Company CRUD.

**Tech Stack:** .NET 10, EF Core, MediatR, FluentValidation, Minimal APIs

---

### Task 1: Add IsDeleted to ExpenseType entity and update EF config

**Files:**
- Modify: `backend/Domain/ExpenseTypes/ExpenseType.cs`
- Modify: `backend/Infrastructure/Models/ExpenseTypeEntity.cs`

- [ ] **Step 1: Add IsDeleted property to ExpenseType**

Modify `backend/Domain/ExpenseTypes/ExpenseType.cs` to add:

```csharp
using Domain.Abstractions;

namespace Domain.ExpenseTypes;

public class ExpenseType : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
```

- [ ] **Step 2: Add global query filter to ExpenseTypeEntity**

Modify `backend/Infrastructure/Models/ExpenseTypeEntity.cs`. Add the query filter after the unique index:

```csharp
builder.HasQueryFilter(e => !e.IsDeleted);
```

The full Configure method should now include this line after `builder.HasIndex(e => e.Name).IsUnique();`.

- [ ] **Step 3: Create and apply migration**

```bash
cd backend/Api/Host && dotnet ef migrations add AddExpenseTypeSoftDelete && dotnet ef database update
```

Expected: Migration created and applied successfully.

- [ ] **Step 4: Commit**

```bash
git add backend/Domain/ExpenseTypes/ExpenseType.cs backend/Infrastructure/Models/ExpenseTypeEntity.cs backend/Infrastructure/Migrations/
git commit -m "feat: add IsDeleted soft-delete to ExpenseType entity"
```

---

### Task 2: Add DeleteExpenses permission

**Files:**
- Modify: `backend/Endpoints/Permissions.cs`

- [ ] **Step 1: Add DeleteExpenses permission constant**

Modify `backend/Endpoints/Permissions.cs` to add:

```csharp
public static string DeleteExpenses = "delete:expenses";
```

Add it after the existing `UpdateExpenses` line.

- [ ] **Step 2: Commit**

```bash
git add backend/Endpoints/Permissions.cs
git commit -m "feat: add DeleteExpenses permission"
```

---

### Task 3: CreateExpenseType command, validator, and handler

**Files:**
- Create: `backend/Application/ExpenseTypes/CreateExpenseTypeCommand.cs`

- [ ] **Step 1: Create the command, validator, and handler**

Create `backend/Application/ExpenseTypes/CreateExpenseTypeCommand.cs`:

```csharp
using Application.Abstractions;
using Domain.Exceptions;
using Domain.ExpenseTypes;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record CreateExpenseTypeCommand(
    string Name,
    string Description
) : ICommand<Guid>;

public class CreateExpenseTypeValidator : AbstractValidator<CreateExpenseTypeCommand>
{
    public CreateExpenseTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class CreateExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<CreateExpenseTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateExpenseTypeCommand request, CancellationToken ct)
    {
        var nameExists = await db.ExpenseTypes
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Name == request.Name, ct);

        if (nameExists)
            throw new DuplicateDomainMemberException($"An expense type with the name '{request.Name}' already exists.");

        var expenseType = new ExpenseType
        {
            Name = request.Name,
            Description = request.Description,
        };

        db.ExpenseTypes.Add(expenseType);
        await db.SaveChangesAsync(ct);

        return expenseType.Id;
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Application/ExpenseTypes/CreateExpenseTypeCommand.cs
git commit -m "feat: add CreateExpenseType command with validation"
```

---

### Task 4: GetExpenseTypeById query and handler

**Files:**
- Create: `backend/Application/ExpenseTypes/GetExpenseTypeByIdQuery.cs`

- [ ] **Step 1: Create the query and handler**

Create `backend/Application/ExpenseTypes/GetExpenseTypeByIdQuery.cs`:

```csharp
using Application.Abstractions;
using Application.DTOs;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record GetExpenseTypeByIdQuery(Guid Id) : IQuery<ExpenseTypeResponse?>;

public class GetExpenseTypeByIdQueryHandler(DashboardDbContext db) : IQueryHandler<GetExpenseTypeByIdQuery, ExpenseTypeResponse?>
{
    public async Task<ExpenseTypeResponse?> Handle(GetExpenseTypeByIdQuery request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        return expenseType == null ? null : ExpenseTypeResponse.FromDomain(expenseType);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Application/ExpenseTypes/GetExpenseTypeByIdQuery.cs
git commit -m "feat: add GetExpenseTypeById query"
```

---

### Task 5: UpdateExpenseType command, validator, and handler

**Files:**
- Create: `backend/Application/ExpenseTypes/UpdateExpenseTypeCommand.cs`

- [ ] **Step 1: Create the command, validator, and handler**

Create `backend/Application/ExpenseTypes/UpdateExpenseTypeCommand.cs`:

```csharp
using Application.Abstractions;
using Domain.Exceptions;
using FluentValidation;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record UpdateExpenseTypeCommand(
    Guid Id,
    string Name,
    string Description
) : ICommand;

public class UpdateExpenseTypeValidator : AbstractValidator<UpdateExpenseTypeCommand>
{
    public UpdateExpenseTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(255);
    }
}

public class UpdateExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateExpenseTypeCommand>
{
    public async Task Handle(UpdateExpenseTypeCommand request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException($"Expense type not found: {request.Id}");

        var nameExists = await db.ExpenseTypes
            .AnyAsync(e => e.Name == request.Name && e.Id != request.Id, ct);

        if (nameExists)
            throw new DuplicateDomainMemberException($"An expense type with the name '{request.Name}' already exists.");

        expenseType.Name = request.Name;
        expenseType.Description = request.Description;
        expenseType.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Application/ExpenseTypes/UpdateExpenseTypeCommand.cs
git commit -m "feat: add UpdateExpenseType command with validation"
```

---

### Task 6: DeleteExpenseType command and handler

**Files:**
- Create: `backend/Application/ExpenseTypes/DeleteExpenseTypeCommand.cs`

- [ ] **Step 1: Create the command and handler**

Create `backend/Application/ExpenseTypes/DeleteExpenseTypeCommand.cs`:

```csharp
using Application.Abstractions;
using Domain.Exceptions;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.ExpenseTypes;

public record DeleteExpenseTypeCommand(Guid Id) : ICommand;

public class DeleteExpenseTypeCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteExpenseTypeCommand>
{
    public async Task Handle(DeleteExpenseTypeCommand request, CancellationToken ct)
    {
        var expenseType = await db.ExpenseTypes
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException($"Expense type not found: {request.Id}");

        expenseType.IsDeleted = true;
        expenseType.UpdatedOn = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Application/ExpenseTypes/DeleteExpenseTypeCommand.cs
git commit -m "feat: add DeleteExpenseType command with soft-delete"
```

---

### Task 7: Add CRUD endpoints to ExpenseEndpoints

**Files:**
- Modify: `backend/Endpoints/Endpoints/ExpenseEndpoints.cs`

- [ ] **Step 1: Add four new endpoints**

Modify `backend/Endpoints/Endpoints/ExpenseEndpoints.cs`. Add these four endpoints inside `MapExpenseEndpoints`, before the `return app;` line:

```csharp
group.MapPost("/expense-types", async (IMediator mediator, CreateExpenseTypeCommand command) =>
    {
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
    .Produces<Application.DTOs.ExpenseTypeResponse>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("GetExpenseTypeById")
    .RequireAuthorization(Permissions.ReadExpenses);

group.MapPut("/expense-types/{id:guid}", async (IMediator mediator, Guid id, UpdateExpenseTypeCommand command) =>
    {
        if (command.Id != id)
            return Results.BadRequest();

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
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add backend/Endpoints/Endpoints/ExpenseEndpoints.cs
git commit -m "feat: add CRUD endpoints for expense types"
```

---

### Task 8: Update GetExpenseTypesQuery to exclude deleted types

**Files:**
- Modify: `backend/Application/ExpenseTypes/GetExpenseTypesQuery.cs`

- [ ] **Step 1: Verify query filter is applied**

Read `backend/Application/ExpenseTypes/GetExpenseTypesQuery.cs`. The global query filter `HasQueryFilter(e => !e.IsDeleted)` added in Task 1 will automatically exclude deleted types from all queries. No code change needed if the query uses `db.ExpenseTypes` normally. Confirm the handler uses `db.ExpenseTypes` without `IgnoreQueryFilters()`.

- [ ] **Step 2: Commit**

```bash
git commit --allow-empty -m "chore: confirm GetExpenseTypesQuery respects soft-delete filter"
```

---

### Task 9: Final verification

**Files:**
- All modified files

- [ ] **Step 1: Full build**

```bash
cd backend && dotnet build
```

Expected: Build succeeds with zero errors and zero new warnings.

- [ ] **Step 2: Verify all new files exist**

```bash
ls backend/Application/ExpenseTypes/
```

Expected output includes:
- `CreateExpenseTypeCommand.cs`
- `GetExpenseTypeByIdQuery.cs`
- `UpdateExpenseTypeCommand.cs`
- `DeleteExpenseTypeCommand.cs`
- `GetExpenseTypesQuery.cs` (existing)

- [ ] **Step 3: Verify endpoint registration**

```bash
grep -n "MapExpenseEndpoints" backend/Endpoints/DependancyInjection.cs
```

Expected: The call exists (it was added in the earlier expense flow standardization work).

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete expense type CRUD API"
```
