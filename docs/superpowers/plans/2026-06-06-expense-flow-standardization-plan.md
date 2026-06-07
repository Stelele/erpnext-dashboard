# Expense Flow Standardization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace hardcoded njeremoto expense types and account mappings with a configurable per-company system backed by the .NET backend, and simplify bulk import to JSON-only.

**Architecture:** Three new backend entities (ExpenseType, CompanyExpenseMapping, CompanySettings) with CRUD endpoints. Frontend fetches mappings/settings on expense page load, populates dropdowns dynamically, and bulk import accepts only JSON.

**Tech Stack:** .NET 10 + EF Core (SQLite), Vue 3 + TypeScript + Pinia

---

## File Structure

### Backend (new files)

| Path | Purpose |
|---|---|
| `backend/Domain/ExpenseTypes/ExpenseType.cs` | Domain entity |
| `backend/Domain/CompanyExpenseMappings/CompanyExpenseMapping.cs` | Domain entity |
| `backend/Domain/CompanySettings/CompanySettings.cs` | Domain entity |
| `backend/Infrastructure/Models/ExpenseTypeEntity.cs` | EF Core config |
| `backend/Infrastructure/Models/CompanyExpenseMappingEntity.cs` | EF Core config |
| `backend/Infrastructure/Models/CompanySettingsEntity.cs` | EF Core config |
| `backend/Application/ExpenseTypes/` | CQRS commands/queries/handlers |
| `backend/Application/CompanyExpenseMappings/` | CQRS commands/queries/handlers |
| `backend/Application/CompanySettings/` | CQRS commands/queries/handlers |
| `backend/Endpoints/Endpoints/ExpenseEndpoints.cs` | Minimal API routes |

### Backend (modified files)

| Path | Change |
|---|---|
| `backend/Infrastructure/Models/DashboardDbContext.cs` | Add 3 new DbSets |
| `backend/Endpoints/DependancyInjection.cs` | Register new endpoint mapper |

### Frontend (modified files)

| Path | Change |
|---|---|
| `frontend/src/types/Expenses.ts` | Remove hardcoded mappings, add dynamic types |
| `frontend/src/services/ErpNextService.ts` | Fetch mappings/settings, dynamic account resolution |
| `frontend/src/services/ExpenseServiceFunctions.ts` | Accept dynamic mappings instead of hardcoded |
| `frontend/src/components/ExpenseForm.vue` | Dynamic dropdown from mappings |
| `frontend/src/components/BulkExpenseUploadButton.vue` | Remove Excel/SQLite, JSON only |
| `frontend/src/views/ExpensesView.vue` | Fetch mappings/settings on load |
| `frontend/src/stores/DataStore.ts` | Store mappings/settings, pass to expense functions |

---

### Task 1: Backend domain entities and EF Core configuration

**Files:**
- Create: `backend/Domain/ExpenseTypes/ExpenseType.cs`
- Create: `backend/Domain/CompanyExpenseMappings/CompanyExpenseMapping.cs`
- Create: `backend/Domain/CompanySettings/CompanySettings.cs`
- Create: `backend/Infrastructure/Models/ExpenseTypeEntity.cs`
- Create: `backend/Infrastructure/Models/CompanyExpenseMappingEntity.cs`
- Create: `backend/Infrastructure/Models/CompanySettingsEntity.cs`
- Modify: `backend/Infrastructure/Models/DashboardDbContext.cs`

- [ ] **Step 1: Create ExpenseType domain entity**

`backend/Domain/ExpenseTypes/ExpenseType.cs`:
```csharp
using NjeremotoDashboard.Domain.Abstractions;

namespace NjeremotoDashboard.Domain.ExpenseTypes;

public class ExpenseType : Base
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create CompanyExpenseMapping domain entity**

`backend/Domain/CompanyExpenseMappings/CompanyExpenseMapping.cs`:
```csharp
using NjeremotoDashboard.Domain.Abstractions;
using NjeremotoDashboard.Domain.Companies;
using NjeremotoDashboard.Domain.ExpenseTypes;

namespace NjeremotoDashboard.Domain.CompanyExpenseMappings;

public class CompanyExpenseMapping : Base
{
    public Guid CompanyId { get; set; }
    public Guid ExpenseTypeId { get; set; }
    public string ErpnextAccountName { get; set; } = string.Empty;

    public Company Company { get; set; } = null!;
    public ExpenseType ExpenseType { get; set; } = null!;
}
```

- [ ] **Step 3: Create CompanySettings domain entity**

`backend/Domain/CompanySettings/CompanySettings.cs`:
```csharp
using NjeremotoDashboard.Domain.Abstractions;
using NjeremotoDashboard.Domain.Companies;

namespace NjeremotoDashboard.Domain.CompanySettings;

public class CompanySettings : Base
{
    public Guid CompanyId { get; set; }
    public string DefaultIncomeAccountName { get; set; } = "Sales";

    public Company Company { get; set; } = null!;
}
```

- [ ] **Step 4: Create ExpenseType EF Core configuration**

`backend/Infrastructure/Models/ExpenseTypeEntity.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NjeremotoDashboard.Domain.ExpenseTypes;

namespace NjeremotoDashboard.Infrastructure.Models;

public class ExpenseTypeEntity : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.ToTable("ExpenseTypes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(255);
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
```

- [ ] **Step 5: Create CompanyExpenseMapping EF Core configuration**

`backend/Infrastructure/Models/CompanyExpenseMappingEntity.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NjeremotoDashboard.Domain.CompanyExpenseMappings;

namespace NjeremotoDashboard.Infrastructure.Models;

public class CompanyExpenseMappingEntity : IEntityTypeConfiguration<CompanyExpenseMapping>
{
    public void Configure(EntityTypeBuilder<CompanyExpenseMapping> builder)
    {
        builder.ToTable("CompanyExpenseMappings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ErpnextAccountName).IsRequired().HasMaxLength(255);
        builder.HasIndex(e => new { e.CompanyId, e.ExpenseTypeId }).IsUnique();

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ExpenseType)
            .WithMany()
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 6: Create CompanySettings EF Core configuration**

`backend/Infrastructure/Models/CompanySettingsEntity.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NjeremotoDashboard.Domain.CompanySettings;

namespace NjeremotoDashboard.Infrastructure.Models;

public class CompanySettingsEntity : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        builder.ToTable("CompanySettings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DefaultIncomeAccountName).IsRequired().HasMaxLength(255);
        builder.HasIndex(e => e.CompanyId).IsUnique();

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 7: Update DashboardDbContext**

Modify `backend/Infrastructure/Models/DashboardDbContext.cs` to add three new DbSets:
```csharp
public DbSet<ExpenseType> ExpenseTypes { get; set; }
public DbSet<CompanyExpenseMapping> CompanyExpenseMappings { get; set; }
public DbSet<CompanySettings> CompanySettings { get; set; }
```

And register the configurations in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ExpenseTypeEntity());
modelBuilder.ApplyConfiguration(new CompanyExpenseMappingEntity());
modelBuilder.ApplyConfiguration(new CompanySettingsEntity());
```

Add the necessary using statements:
```csharp
using NjeremotoDashboard.Domain.CompanyExpenseMappings;
using NjeremotoDashboard.Domain.CompanySettings;
using NjeremotoDashboard.Domain.ExpenseTypes;
```

- [ ] **Step 8: Create and apply migration**

```bash
cd backend/Api/Host
dotnet ef migrations add AddExpenseMappingsAndSettings
dotnet ef database update
```

Expected: Migration created and applied successfully.

- [ ] **Step 9: Seed standard expense types**

Add seed data to `ExpenseTypeEntity.cs` in the Configure method:
```csharp
builder.HasData(
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000001"), Name = "Utilities", Description = "Utility expenses" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000002"), Name = "Consumables", Description = "Consumable supplies" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000003"), Name = "Administrative", Description = "Administrative expenses" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000004"), Name = "Entertainment", Description = "Entertainment expenses" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000005"), Name = "Maintenance", Description = "Maintenance costs" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000006"), Name = "Rent", Description = "Rent payments" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000007"), Name = "Travel", Description = "Travel expenses" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000008"), Name = "Office Supplies", Description = "Office supplies" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000009"), Name = "Professional Fees", Description = "Professional service fees" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000010"), Name = "Staff Welfare", Description = "Staff welfare expenses" },
    new ExpenseType { Id = Guid.Parse("00000000-0000-0000-0001-000000000011"), Name = "Other", Description = "Other expenses" }
);
```

Then update the migration:
```bash
dotnet ef migrations add SeedExpenseTypes
dotnet ef database update
```

- [ ] **Step 10: Commit**

```bash
git add backend/
git commit -m "feat: add ExpenseType, CompanyExpenseMapping, CompanySettings entities with seed data"
```

---

### Task 2: Backend DTOs and CQRS handlers

**Files:**
- Create: `backend/Application/DTOs/ExpenseTypeResponse.cs`
- Create: `backend/Application/DTOs/CompanyExpenseMappingResponse.cs`
- Create: `backend/Application/DTOs/CompanySettingsResponse.cs`
- Create: `backend/Application/ExpenseTypes/GetExpenseTypesQuery.cs`
- Create: `backend/Application/CompanyExpenseMappings/GetCompanyExpenseMappingsQuery.cs`
- Create: `backend/Application/CompanyExpenseMappings/UpsertCompanyExpenseMappingsCommand.cs`
- Create: `backend/Application/CompanySettings/GetCompanySettingsQuery.cs`
- Create: `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`

- [ ] **Step 1: Create DTOs**

`backend/Application/DTOs/ExpenseTypeResponse.cs`:
```csharp
using NjeremotoDashboard.Domain.ExpenseTypes;

namespace NjeremotoDashboard.Application.DTOs;

public record ExpenseTypeResponse(Guid Id, string Name, string Description)
{
    public static ExpenseTypeResponse FromDomain(ExpenseType type) =>
        new(type.Id, type.Name, type.Description);
}
```

`backend/Application/DTOs/CompanyExpenseMappingResponse.cs`:
```csharp
using NjeremotoDashboard.Domain.CompanyExpenseMappings;

namespace NjeremotoDashboard.Application.DTOs;

public record CompanyExpenseMappingResponse(
    Guid Id,
    Guid ExpenseTypeId,
    string ExpenseTypeName,
    string ErpnextAccountName
)
{
    public static CompanyExpenseMappingResponse FromDomain(CompanyExpenseMapping mapping, string expenseTypeName) =>
        new(mapping.Id, mapping.ExpenseTypeId, expenseTypeName, mapping.ErpnextAccountName);
}
```

`backend/Application/DTOs/CompanySettingsResponse.cs`:
```csharp
using NjeremotoDashboard.Domain.CompanySettings;

namespace NjeremotoDashboard.Application.DTOs;

public record CompanySettingsResponse(Guid Id, Guid CompanyId, string DefaultIncomeAccountName)
{
    public static CompanySettingsResponse FromDomain(CompanySettings settings) =>
        new(settings.Id, settings.CompanyId, settings.DefaultIncomeAccountName);
}
```

- [ ] **Step 2: Create GetExpenseTypesQuery**

`backend/Application/ExpenseTypes/GetExpenseTypesQuery.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using NjeremotoDashboard.Application.Abstractions;
using NjeremotoDashboard.Application.DTOs;
using NjeremotoDashboard.Infrastructure.Models;

namespace NjeremotoDashboard.Application.ExpenseTypes;

public record GetExpenseTypesQuery : IQuery<List<ExpenseTypeResponse>>;

public class GetExpenseTypesQueryHandler(DashboardDbContext db) : IQueryHandler<GetExpenseTypesQuery, List<ExpenseTypeResponse>>
{
    public async Task<List<ExpenseTypeResponse>> Handle(GetExpenseTypesQuery request, CancellationToken ct)
    {
        return await db.ExpenseTypes
            .OrderBy(e => e.Name)
            .Select(e => ExpenseTypeResponse.FromDomain(e))
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 3: Create GetCompanyExpenseMappingsQuery**

`backend/Application/CompanyExpenseMappings/GetCompanyExpenseMappingsQuery.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using NjeremotoDashboard.Application.Abstractions;
using NjeremotoDashboard.Application.DTOs;
using NjeremotoDashboard.Infrastructure.Models;

namespace NjeremotoDashboard.Application.CompanyExpenseMappings;

public record GetCompanyExpenseMappingsQuery(Guid CompanyId) : IQuery<List<CompanyExpenseMappingResponse>>;

public class GetCompanyExpenseMappingsQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanyExpenseMappingsQuery, List<CompanyExpenseMappingResponse>>
{
    public async Task<List<CompanyExpenseMappingResponse>> Handle(GetCompanyExpenseMappingsQuery request, CancellationToken ct)
    {
        return await db.CompanyExpenseMappings
            .Include(m => m.ExpenseType)
            .Where(m => m.CompanyId == request.CompanyId)
            .Select(m => CompanyExpenseMappingResponse.FromDomain(m, m.ExpenseType.Name))
            .ToListAsync(ct);
    }
}
```

- [ ] **Step 4: Create UpsertCompanyExpenseMappingsCommand**

`backend/Application/CompanyExpenseMappings/UpsertCompanyExpenseMappingsCommand.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using NjeremotoDashboard.Application.Abstractions;
using NjeremotoDashboard.Infrastructure.Models;

namespace NjeremotoDashboard.Application.CompanyExpenseMappings;

public record UpsertCompanyExpenseMappingsCommand(
    Guid CompanyId,
    List<MappingItem> Mappings
) : ICommand;

public record MappingItem(Guid ExpenseTypeId, string ErpnextAccountName);

public class UpsertCompanyExpenseMappingsCommandHandler(DashboardDbContext db) : ICommandHandler<UpsertCompanyExpenseMappingsCommand>
{
    public async Task Handle(UpsertCompanyExpenseMappingsCommand request, CancellationToken ct)
    {
        var existing = await db.CompanyExpenseMappings
            .Where(m => m.CompanyId == request.CompanyId)
            .ToListAsync(ct);

        db.CompanyExpenseMappings.RemoveRange(existing);

        var newMappings = request.Mappings.Select(m => new Domain.CompanyExpenseMappings.CompanyExpenseMapping
        {
            CompanyId = request.CompanyId,
            ExpenseTypeId = m.ExpenseTypeId,
            ErpnextAccountName = m.ErpnextAccountName,
        }).ToList();

        await db.CompanyExpenseMappings.AddRangeAsync(newMappings, ct);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Create GetCompanySettingsQuery**

`backend/Application/CompanySettings/GetCompanySettingsQuery.cs`:
```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using NjeremotoDashboard.Application.Abstractions;
using NjeremotoDashboard.Application.DTOs;
using NjeremotoDashboard.Infrastructure.Models;

namespace NjeremotoDashboard.Application.CompanySettings;

public record GetCompanySettingsQuery(Guid CompanyId) : IQuery<CompanySettingsResponse?>;

public class GetCompanySettingsQueryHandler(DashboardDbContext db) : IQueryHandler<GetCompanySettingsQuery, CompanySettingsResponse?>
{
    public async Task<CompanySettingsResponse?> Handle(GetCompanySettingsQuery request, CancellationToken ct)
    {
        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        return settings == null ? null : CompanySettingsResponse.FromDomain(settings);
    }
}
```

- [ ] **Step 6: Create UpdateCompanySettingsCommand**

`backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using NjeremotoDashboard.Application.Abstractions;
using NjeremotoDashboard.Domain.CompanySettings;
using NjeremotoDashboard.Infrastructure.Models;

namespace NjeremotoDashboard.Application.CompanySettings;

public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName
) : ICommand;

public class UpdateCompanySettingsCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateCompanySettingsCommand>
{
    public async Task Handle(UpdateCompanySettingsCommand request, CancellationToken ct)
    {
        var settings = await db.CompanySettings
            .FirstOrDefaultAsync(s => s.CompanyId == request.CompanyId, ct);

        if (settings == null)
        {
            settings = new CompanySettings
            {
                CompanyId = request.CompanyId,
                DefaultIncomeAccountName = request.DefaultIncomeAccountName,
            };
            db.CompanySettings.Add(settings);
        }
        else
        {
            settings.DefaultIncomeAccountName = request.DefaultIncomeAccountName;
            db.CompanySettings.Update(settings);
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add backend/Application/
git commit -m "feat: add CQRS handlers for expense types, mappings, and settings"
```

---

### Task 3: Backend API endpoints

**Files:**
- Create: `backend/Endpoints/Endpoints/ExpenseEndpoints.cs`
- Modify: `backend/Endpoints/DependancyInjection.cs`

- [ ] **Step 1: Create ExpenseEndpoints.cs**

`backend/Endpoints/Endpoints/ExpenseEndpoints.cs`:
```csharp
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NjeremotoDashboard.Application.CompanyExpenseMappings;
using NjeremotoDashboard.Application.CompanySettings;
using NjeremotoDashboard.Application.ExpenseTypes;

namespace NjeremotoDashboard.Endpoints.Endpoints;

public static class ExpenseEndpoints
{
    public static WebApplication MapExpenseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags(Tags.Expenses);

        group.MapGet("/expense-types", async (IMediator mediator) =>
            await mediator.Send(new GetExpenseTypesQuery()))
            .Produces<List<Application.DTOs.ExpenseTypeResponse>>();

        group.MapGet("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, IMediator mediator) =>
            await mediator.Send(new GetCompanyExpenseMappingsQuery(companyId)))
            .Produces<List<Application.DTOs.CompanyExpenseMappingResponse>>();

        group.MapPut("/companies/{companyId:guid}/expense-mappings", async (Guid companyId, UpsertCompanyExpenseMappingsCommand command, IMediator mediator) =>
        {
            if (command.CompanyId != companyId)
                return Results.BadRequest();

            await mediator.Send(command);
            return Results.NoContent();
        })
        .Produces(204);

        group.MapGet("/companies/{companyId:guid}/settings", async (Guid companyId, IMediator mediator) =>
        {
            var settings = await mediator.Send(new GetCompanySettingsQuery(companyId));
            return settings == null ? Results.NotFound() : Results.Ok(settings);
        })
        .Produces<Application.DTOs.CompanySettingsResponse>()
        .Produces(404);

        group.MapPut("/companies/{companyId:guid}/settings", async (Guid companyId, UpdateCompanySettingsCommand command, IMediator mediator) =>
        {
            if (command.CompanyId != companyId)
                return Results.BadRequest();

            await mediator.Send(command);
            return Results.NoContent();
        })
        .Produces(204);

        return app;
    }
}
```

- [ ] **Step 2: Add Expenses tag**

Modify `backend/Endpoints/Tags.cs` to add:
```csharp
public const string Expenses = "Expenses";
```

- [ ] **Step 3: Register expense endpoints**

Modify `backend/Endpoints/DependancyInjection.cs` to add:
```csharp
app.MapExpenseEndpoints()
```

Add the using statement:
```csharp
using NjeremotoDashboard.Endpoints.Endpoints;
```

- [ ] **Step 4: Commit**

```bash
git add backend/Endpoints/
git commit -m "feat: add expense endpoints for types, mappings, and settings"
```

---

### Task 4: Frontend types and service layer

**Files:**
- Modify: `frontend/src/types/Expenses.ts`
- Modify: `frontend/src/services/ErpNextService.ts`
- Modify: `frontend/src/services/ExpenseServiceFunctions.ts`

- [ ] **Step 1: Update Expenses.ts**

Replace the entire `frontend/src/types/Expenses.ts` with:
```typescript
export interface ExpenseType {
  id: string;
  name: string;
  description: string;
}

export interface CompanyExpenseMapping {
  id: string;
  expenseTypeId: string;
  expenseTypeName: string;
  erpnextAccountName: string;
}

export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
}

export interface Expense {
  date: string;
  expenseTypeId: string;
  amount: number;
  description: string;
}

export interface AccountResponse {
  name: string;
  account_name: string;
}

export interface Payment {
  id: string;
  date: string;
  status: "Draft" | "Submitted" | "Cancelled";
  type: "Expense" | "Order";
  description: string;
  amount: number;
  account?: string;
}

export interface AccountMappings {
  expenses: Record<string, AccountResponse>; // expenseTypeId -> AccountResponse
  income: AccountResponse | null;
}
```

- [ ] **Step 2: Update ErpNextService.ts**

Add new methods and update existing ones:

Add these imports at the top:
```typescript
import type { ExpenseType, CompanyExpenseMapping, CompanySettings, AccountMappings, AccountResponse } from "@/types/Expenses";
```

Add these new methods to the `ErpNextService` class:
```typescript
public getExpenseTypes() {
  return this.instance
    .get<ExpenseType[]>("/api/expense-types")
    .then((resp) => resp?.data);
}

public getCompanyExpenseMappings(companyId: string) {
  return this.instance
    .get<CompanyExpenseMapping[]>(`/api/companies/${companyId}/expense-mappings`)
    .then((resp) => resp?.data);
}

public upsertCompanyExpenseMappings(companyId: string, mappings: { expenseTypeId: string; erpnextAccountName: string }[]) {
  return this.instance
    .put(`/api/companies/${companyId}/expense-mappings`, {
      companyId,
      mappings,
    });
}

public getCompanySettings(companyId: string) {
  return this.instance
    .get<CompanySettings>(`/api/companies/${companyId}/settings`)
    .then((resp) => resp?.data);
}

public updateCompanySettings(companyId: string, settings: { defaultIncomeAccountName: string }) {
  return this.instance
    .put(`/api/companies/${companyId}/settings`, {
      companyId,
      ...settings,
    });
}
```

Replace the `getAccountMappings` method with:
```typescript
public async getAccountMappings(
  expenseMappings: CompanyExpenseMapping[],
  incomeAccountName: string,
): Promise<AccountMappings> {
  const accounts = await this.getAllAccounts();

  const expenses: Record<string, AccountResponse> = {};
  for (const mapping of expenseMappings) {
    const account = accounts.expense.find(
      (a) => a.account_name === mapping.erpnextAccountName,
    );
    if (account) {
      expenses[mapping.expenseTypeId] = account;
    }
  }

  const income = accounts.income.find(
    (a) => a.account_name === incomeAccountName,
  ) ?? null;

  return { expenses, income };
}
```

Replace `addDraftExpenseJournalEntry` to use `expense.expenseTypeId` instead of `expense.expenseType`:
```typescript
public async addDraftExpenseJournalEntry(
  expense: Expense,
  incomeAccount: AccountResponse,
  expenseAccount: AccountResponse,
) {
  const body = {
    voucher_type: "Journal Entry",
    company: authStore.company,
    posting_date: expense.date,
    user_remark: expense.description,
    accounts: [
      {
        account: expenseAccount.name,
        debit_in_account_currency: expense.amount,
      },
      {
        account: incomeAccount.name,
        credit_in_account_currency: expense.amount,
      },
    ],
  };

  try {
    const response = await this.instance.post<{ data: JournalEntry }>(
      "/api/resource/Journal Entry",
      body,
    );
    return response.data.data;
  } catch {
    return undefined;
  }
}
```

- [ ] **Step 3: Update ExpenseServiceFunctions.ts**

Replace the entire `frontend/src/services/ExpenseServiceFunctions.ts` with:
```typescript
import type { AccountMappings, Expense } from "@/types/Expenses";
import type { ErpNextService } from "./ErpNextService";
import type { JournalEntry } from "@/types/ErpNext";

export interface ExpenseSubmissionResult {
  success: boolean;
  expense: Expense;
  error?: Error;
}

function addDraftExpense(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expense: Expense,
): Promise<JournalEntry | undefined> {
  const incomeAccount = accountMappings.income;
  const expenseAccount = accountMappings.expenses[expense.expenseTypeId];

  if (!incomeAccount || !expenseAccount) {
    return Promise.resolve(undefined);
  }

  return erpNextService.addDraftExpenseJournalEntry(
    expense,
    incomeAccount,
    expenseAccount,
  );
}

async function bulkAddDraftExpenses(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expenses: Expense[],
): Promise<ExpenseSubmissionResult[]> {
  const batchSize = 20;
  const results: ExpenseSubmissionResult[] = [];

  for (let i = 0; i < expenses.length; i += batchSize) {
    const batch = expenses.slice(i, i + batchSize);
    const batchResults = await Promise.allSettled(
      batch.map(async (expense) => {
        const response = await addDraftExpense(
          erpNextService,
          accountMappings,
          expense,
        );
        return { expense, response };
      }),
    );

    for (const result of batchResults) {
      if (result.status === "fulfilled") {
        results.push({
          success: result.value.response !== undefined,
          expense: result.value.expense,
          error: result.value.response === undefined
            ? new Error("Journal entry creation failed")
            : undefined,
        });
      } else {
        results.push({
          success: false,
          expense: (result.reason as any)?.expense ?? {} as Expense,
          error: result.reason as Error,
        });
      }
    }
  }

  return results;
}

export { addDraftExpense, bulkAddDraftExpenses };
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/Expenses.ts frontend/src/services/
git commit -m "refactor: update frontend types and services for dynamic expense mappings"
```

---

### Task 5: Frontend UI components

**Files:**
- Modify: `frontend/src/components/ExpenseForm.vue`
- Modify: `frontend/src/components/BulkExpenseUploadButton.vue`
- Modify: `frontend/src/views/ExpensesView.vue`
- Modify: `frontend/src/stores/DataStore.ts`

- [ ] **Step 1: Update ExpenseForm.vue**

Replace the hardcoded expense type dropdown with dynamic types. The component should:
- Accept `expenseTypes: ExpenseType[]` as a prop
- Accept `mappings: CompanyExpenseMapping[]` as a prop
- Dropdown shows `mapping.expenseTypeName` for each mapping that has an ERPNext account configured
- Emits `expenseTypeId` instead of `expenseType`

```vue
<script setup lang="ts">
import { ref } from "vue";
import type { ExpenseType, CompanyExpenseMapping } from "@/types/Expenses";
import type { Expense } from "@/types/Expenses";

const props = defineProps<{
  expenseTypes: ExpenseType[];
  mappings: CompanyExpenseMapping[];
}>();

const emit = defineEmits<{
  onSubmit: [expense: Expense];
}>();

const date = ref(new Date());
const expenseTypeId = ref("");
const amount = ref(0);
const description = ref("");

const configuredMappings = props.mappings.filter(
  (m) => m.erpnextAccountName && m.erpnextAccountName.trim() !== "",
);

function submit() {
  emit("onSubmit", {
    date: date.value.toISOString().split("T")[0],
    expenseTypeId: expenseTypeId.value,
    amount: amount.value,
    description: description.value,
  });
}
</script>

<template>
  <UForm @submit="submit">
    <UFormField label="Date">
      <UPopover>
        <UButton :label="date.toLocaleDateString()" />
        <template #content>
          <UCalendar v-model="date" />
        </template>
      </UPopover>
    </UFormField>

    <UFormField label="Expense Type">
      <USelect
        v-model="expenseTypeId"
        :items="configuredMappings.map((m) => ({ label: m.expenseTypeName, value: m.expenseTypeId }))"
        placeholder="Select expense type"
      />
    </UFormField>

    <UFormField label="Amount">
      <UInputNumber v-model="amount" :min="0.01" :step="0.01" />
    </UFormField>

    <UFormField label="Description">
      <UTextarea v-model="description" :rows="3" />
    </UFormField>

    <UButton type="submit" label="Submit" />
  </UForm>
</template>
```

- [ ] **Step 2: Update BulkExpenseUploadButton.vue**

Remove Excel and SQLite parsing. Keep only JSON:

```vue
<script setup lang="ts">
import type { Expense } from "@/types/Expenses";

const emit = defineEmits<{
  onDataExtracted: [expenses: Expense[]];
}>();

function handleFile(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const json = JSON.parse(e.target?.result as string);
      const expenses: Expense[] = (json as any[]).map((item: any) => ({
        date: item.date,
        expenseTypeId: item.expenseTypeId,
        amount: Number(item.amount),
        description: item.description ?? "",
      }));
      emit("onDataExtracted", expenses);
    } catch {
      console.error("Failed to parse JSON file");
    }
  };
  reader.readAsText(file);
}
</script>

<template>
  <label class="cursor-pointer">
    <input type="file" accept=".json" class="hidden" @change="handleFile" />
    <UButton label="Upload JSON" icon="i-lucide-upload" />
  </label>
</template>
```

- [ ] **Step 3: Update ExpensesView.vue**

Update to fetch expense types, mappings, and settings on load:

```vue
<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useDataStore } from "@/stores/DataStore";
import type { ExpenseType, CompanyExpenseMapping, CompanySettings } from "@/types/Expenses";
import type { Expense } from "@/types/Expenses";
import ExpenseForm from "@/components/ExpenseForm.vue";
import BulkExpenseUploadButton from "@/components/BulkExpenseUploadButton.vue";
import BulkExpensePreview from "@/components/BulkExpensePreview.vue";

const dataStore = useDataStore();
const expenseTypes = ref<ExpenseType[]>([]);
const mappings = ref<CompanyExpenseMapping[]>([]);
const settings = ref<CompanySettings | null>(null);

onMounted(async () => {
  const authStore = useAuthStore();
  expenseTypes.value = await dataStore.getExpenseTypes();
  mappings.value = await dataStore.getCompanyExpenseMappings(authStore.company.id);
  settings.value = await dataStore.getCompanySettings(authStore.company.id);
  await dataStore.initAccountMappings(mappings.value, settings.value?.defaultIncomeAccountName ?? "Sales");
});

async function onSubmit(expense: Expense) {
  const response = await dataStore.addDraftExpense(expense);
  if (response) {
    // success toast
  } else {
    // error toast
  }
}

async function onBulkSubmit(expenses: Expense[]) {
  const results = await dataStore.bulkAddDraftExpenses(expenses);
  // toast results
}
</script>
```

- [ ] **Step 4: Update DataStore.ts**

Add methods for fetching expense types, mappings, and settings:

```typescript
const expenseTypes = ref<ExpenseType[]>([]);
const mappings = ref<CompanyExpenseMapping[]>([]);
const settings = ref<CompanySettings | null>(null);

async function getExpenseTypes(): Promise<ExpenseType[]> {
  const service = new ErpNextService();
  expenseTypes.value = await service.getExpenseTypes();
  return expenseTypes.value;
}

async function getCompanyExpenseMappings(companyId: string): Promise<CompanyExpenseMapping[]> {
  const service = new ErpNextService();
  mappings.value = await service.getCompanyExpenseMappings(companyId);
  return mappings.value;
}

async function getCompanySettings(companyId: string): Promise<CompanySettings | null> {
  const service = new ErpNextService();
  settings.value = await service.getCompanySettings(companyId);
  return settings.value;
}

async function initAccountMappings(
  expenseMappings: CompanyExpenseMapping[],
  incomeAccountName: string,
) {
  const service = new ErpNextService();
  accountMappings.value = await service.getAccountMappings(
    expenseMappings,
    incomeAccountName,
  );
}

function addDraftExpense(expense: Expense) {
  return ExpenseServiceFunctions.addDraftExpense(
    new ErpNextService(),
    accountMappings.value,
    expense,
  );
}

async function bulkAddDraftExpenses(expenses: Expense[]) {
  return ExpenseServiceFunctions.bulkAddDraftExpenses(
    new ErpNextService(),
    accountMappings.value,
    expenses,
  );
}
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/ frontend/src/views/ frontend/src/stores/
git commit -m "feat: update expense UI components for dynamic mappings, JSON-only bulk import"
```

---

### Task 6: Verify and test

**Files:**
- All modified files

- [ ] **Step 1: Build backend**

```bash
cd backend/Api/Host && dotnet build
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Build frontend**

```bash
cd frontend && npm run build
```

Expected: Build succeeds with no errors.

- [ ] **Step 3: Verify directory structure**

```bash
find backend/Domain backend/Application backend/Endpoints backend/Infrastructure/Models -name "*.cs" | grep -i expense
find frontend/src -name "*.ts" -o -name "*.vue" | grep -i expense
```

Expected: All new files present.

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete expense flow standardization"
```
