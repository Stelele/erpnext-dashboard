# DTO Structure Refactoring Design

**Date:** 2026-06-07

## Problem

1. **Response DTO inheritance is unnecessary** — `Base*Response` classes exist only to share fields, not for polymorphism. `Extended*Response` variants were created anticipating different list/detail shapes, but the data is small enough that full responses everywhere are fine.

2. **Commands used directly as API request bodies** — Route parameters (e.g., `{id}`, `{companyId}`) are duplicated inside command records, leading to manual validation in endpoints like `if (command.Id != id) return Results.BadRequest();`.

## Response DTOs

**Delete:**
- `BaseCompanyResponse`, `ExtendedCompanyResponse`
- `BaseSiteResponse`, `ExtendedSiteResponse`
- `BaseUserResponse`, `ExtendedUserResponse`
- `SiteCompanyResponse` (one-off, no longer needed)

**Keep and flatten** — one flat response DTO per entity with nested objects included:

- `CompanyResponse` — `Id`, `Name`, `Description`, `Site` (nested `SiteResponse`)
- `SiteResponse` — `Id`, `Name`, `Description`, `Url`, `ApiToken`, `Companies` (List<Guid>)
- `UserResponse` — `Id`, `Name`, `Email`, `Companies` (List<CompanyResponse>)
- `ExpenseTypeResponse` — unchanged (already clean)
- `CompanySettingsResponse` — unchanged (already clean)
- `CompanyExpenseMappingResponse` — unchanged (already clean)

## Request DTOs (new)

Create in `backend/Api/Requests/`:

| Endpoint | Request DTO | Fields |
|----------|-------------|--------|
| POST `/companies` | `CreateCompanyRequest` | `SiteId`, `Name`, `Description` |
| POST `/sites` | `CreateSiteRequest` | `Name`, `Url`, `Description`, `ApiToken` |
| POST `/users` | `CreateUserRequest` | `Name`, `Email` |
| POST `/expense-types` | `CreateExpenseTypeRequest` | `Name`, `Description` |
| PUT `/expense-types/{id}` | `UpdateExpenseTypeRequest` | `Name`, `Description` |
| PUT `/companies/{companyId}/expense-mappings` | `UpsertCompanyExpenseMappingsRequest` | `Mappings` |
| PUT `/companies/{companyId}/settings` | `UpdateCompanySettingsRequest` | `DefaultIncomeAccountName` |

Request DTOs do NOT contain route parameters. Endpoints construct commands from route params + request body.

## Endpoint Mapping Example

```csharp
// Before
app.MapPut("/expense-types/{id:guid}", async (IMediator m, Guid id, UpdateExpenseTypeCommand cmd) => {
    if (cmd.Id != id) return Results.BadRequest();
    await m.Send(cmd);
    return Results.NoContent();
});

// After
app.MapPut("/expense-types/{id:guid}", async (IMediator m, Guid id, UpdateExpenseTypeRequest req) => {
    var command = new UpdateExpenseTypeCommand(id, req.Name, req.Description);
    await m.Send(command);
    return Results.NoContent();
});
```

## File Structure After

```
backend/
  Api/
    Requests/
      CreateCompanyRequest.cs
      CreateSiteRequest.cs
      CreateUserRequest.cs
      CreateExpenseTypeRequest.cs
      UpdateExpenseTypeRequest.cs
      UpsertCompanyExpenseMappingsRequest.cs
      UpdateCompanySettingsRequest.cs
  Application/
    DTOs/
      CompanyResponse.cs          (flattened, no inheritance)
      SiteResponse.cs             (flattened, no inheritance)
      UserResponse.cs             (flattened, no inheritance)
      ExpenseTypeResponse.cs      (unchanged)
      CompanySettingsResponse.cs  (unchanged)
      CompanyExpenseMappingResponse.cs (unchanged)
```

## Migration Steps

1. Create `Api/Requests/` and all request DTOs
2. Update endpoints to bind request DTOs and construct commands
3. Flatten response DTOs — remove Base/Extended classes
4. Update all `FromDomain` methods and query handlers
5. Update `.Produces<T>()` declarations
6. Remove old Base/Extended response classes
7. Build and verify
