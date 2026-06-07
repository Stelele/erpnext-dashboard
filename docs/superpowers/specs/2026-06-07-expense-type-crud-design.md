# Expense Type CRUD API Design

**Date:** 2026-06-07
**Status:** Approved

## Problem

The backend has seeded expense types but no API to create, update, or delete custom expense types. Users cannot add company-specific expense categories.

## Architecture

Build on existing CQRS pattern. Four new/modified files in Application layer, one endpoint file modified, one entity modified, one permissions file modified.

## Entity Change

Add `IsDeleted` property to `Domain.ExpenseTypes.ExpenseType`:

```csharp
public bool IsDeleted { get; set; }
```

Update `ExpenseTypeEntity.cs` to add global query filter:
```csharp
builder.HasQueryFilter(e => !e.IsDeleted);
```

Add migration for the new column.

## New Endpoints

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/api/expense-types` | `update:expenses` | Create new expense type |
| GET | `/api/expense-types/{id}` | `read:expenses` | Get single expense type |
| PUT | `/api/expense-types/{id}` | `update:expenses` | Update name/description |
| DELETE | `/api/expense-types/{id}` | `update:expenses` | Soft-delete expense type |

## Application Layer

### CreateExpenseTypeCommand
- Record: `CreateExpenseTypeCommand(string Name, string Description) : ICommand<Guid>`
- Validator: Name required, max 100 chars, unique. Description max 255 chars.
- Handler: Creates entity, saves, returns new Id.

### GetExpenseTypeByIdQuery
- Record: `GetExpenseTypeByIdQuery(Guid Id) : IQuery<ExpenseTypeResponse?>`
- Handler: Returns null if not found or deleted.

### UpdateExpenseTypeCommand
- Record: `UpdateExpenseTypeCommand(Guid Id, string Name, string Description) : ICommand`
- Validator: Same as create, plus Id must exist.
- Handler: Finds entity, updates fields, saves.

### DeleteExpenseTypeCommand
- Record: `DeleteExpenseTypeCommand(Guid Id) : ICommand`
- Validator: Id must exist.
- Handler: Sets `IsDeleted = true`, saves.

## Permissions

Add to `Permissions.cs`:
```csharp
public static string DeleteExpenses = "delete:expenses";
```

DELETE endpoint uses `DeleteExpenses`, others use existing `ReadExpenses`/`UpdateExpenses`.

## Files Changed

| File | Action |
|------|--------|
| `backend/Domain/ExpenseTypes/ExpenseType.cs` | Add IsDeleted property |
| `backend/Infrastructure/Models/ExpenseTypeEntity.cs` | Add query filter, update seed |
| `backend/Application/ExpenseTypes/CreateExpenseTypeCommand.cs` | New |
| `backend/Application/ExpenseTypes/GetExpenseTypeByIdQuery.cs` | New |
| `backend/Application/ExpenseTypes/UpdateExpenseTypeCommand.cs` | New |
| `backend/Application/ExpenseTypes/DeleteExpenseTypeCommand.cs` | New |
| `backend/Endpoints/Endpoints/ExpenseEndpoints.cs` | Add 4 endpoints |
| `backend/Endpoints/Permissions.cs` | Add DeleteExpenses |

## Error Handling

- 404: Expense type not found (GET, PUT, DELETE)
- 409: Duplicate name on create/update
- 400: Validation failures (FluentValidation pipeline handles)
- 401/403: Unauthorized/Forbidden (Authorization middleware handles)

## Migration

New EF Core migration adds `IsDeleted` column to `ExpenseTypes` table.
