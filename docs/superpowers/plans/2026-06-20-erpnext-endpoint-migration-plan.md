# ERPNext Endpoint Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace manually-entered ERPNext server scripts with the `awesome_dashboard` Frappe app endpoints, and make njeremoto-specific behaviors (pack-size maps, account filtering) per-company configurable settings.

**Architecture:** Expand `CompanySettings` with `PackSizeMap` and `AccountFilters` JSON columns; update 19 endpoint paths in `ErpNextService.ts` from `/api/v2/method/<script>` to `/api/method/awesome_dashboard.api.<module>.<fn>`; wire settings from DataStore into ErpNextService; remove old `erpnext/` scripts directory.

**Tech Stack:** .NET 10 / EF Core SQLite / Vue 3 / TypeScript

---

### Task 1: Backend Domain + EF Configuration

**Files:**
- Modify: `backend/Domain/CompanySettings/CompanySettings.cs`
- Modify: `backend/Infrastructure/Models/CompanySettingsEntity.cs`

- [ ] **Step 1: Add PackSizeMap and AccountFilters to domain entity**

In `backend/Domain/CompanySettings/CompanySettings.cs`, add two properties after `ThemeMode`:

```csharp
public string? PackSizeMap { get; set; }
public string? AccountFilters { get; set; }
```

- [ ] **Step 2: Add EF column configuration**

In `backend/Infrastructure/Models/CompanySettingsEntity.cs`, add after the `ThemeMode` configuration:

```csharp
builder
    .Property(e => e.PackSizeMap)
    .HasMaxLength(4000);

builder
    .Property(e => e.AccountFilters)
    .HasMaxLength(4000);
```

- [ ] **Step 3: Build to verify domain compiles**

Run: `dotnet build backend/Host/Host.csproj`
Expected: Build succeeds (migration warning may appear — that's OK, the next task creates it).

---

### Task 2: Backend Migration

**Files:**
- Create: `backend/Infrastructure/Migrations/XXXXXX_AddPackSizeMapAndAccountFilters.cs`
- Create: `backend/Infrastructure/Migrations/XXXXXX_AddPackSizeMapAndAccountFilters.Designer.cs`
- Modify: `backend/Infrastructure/Migrations/DashboardDbContextModelSnapshot.cs`

- [ ] **Step 1: Generate EF migration**

Run:
```bash
dotnet ef migrations add AddPackSizeMapAndAccountFilters --project backend/Infrastructure/Infrastructure.csproj --startup-project backend/Host/Host.csproj
```
Expected: Creates migration files. Actual timestamp prefix will vary.

- [ ] **Step 2: Verify migration SQL is correct**

Run:
```bash
dotnet ef migrations script --project backend/Infrastructure/Infrastructure.csproj --startup-project backend/Host/Host.csproj
```
Expected: The latest migration should show `ALTER TABLE "CompanySettings" ADD COLUMN "PackSizeMap" TEXT` and `ALTER TABLE "CompanySettings" ADD COLUMN "AccountFilters" TEXT`.

- [ ] **Step 3: Run migration against local database**

Run:
```bash
dotnet ef database update --project backend/Infrastructure/Infrastructure.csproj --startup-project backend/Host/Host.csproj
```
Expected: "Done."

---

### Task 3: Backend Application Layer + Endpoints

**Files:**
- Modify: `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`
- Modify: `backend/Application/DTOs/CompanySettingsResponse.cs`
- Modify: `backend/Application/Requests/UpdateCompanySettingsRequest.cs`
- Modify: `backend/Endpoints/Endpoints/CompanyEndpoints.cs`

- [ ] **Step 1: Update UpdateCompanySettingsCommand**

In `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs`, add the two new fields to the record:

```csharp
[InvalidateCache(Category = "settings")]
public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null,
    string? PackSizeMap = null,
    string? AccountFilters = null
) : ICommand;
```

In the handler, update the two code paths (create and update) to include the new fields. In the `settings == null` block, add after `ThemeMode`:

```csharp
PackSizeMap = request.PackSizeMap,
AccountFilters = request.AccountFilters,
```

In the `else` block, add after `settings.ThemeMode = request.ThemeMode;`:

```csharp
settings.PackSizeMap = request.PackSizeMap;
settings.AccountFilters = request.AccountFilters;
```

- [ ] **Step 2: Update CompanySettingsResponse DTO**

In `backend/Application/DTOs/CompanySettingsResponse.cs`, add the new fields to the record:

```csharp
public record CompanySettingsResponse(
    Guid Id,
    Guid CompanyId,
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor,
    NeutralColor? NeutralColor,
    ThemeMode? ThemeMode,
    string? PackSizeMap,
    string? AccountFilters
)
```

Update the `FromDomain` factory method to include the new fields:

```csharp
public static CompanySettingsResponse FromDomain(CompanySettingsEntity settings) =>
    new(
        settings.Id,
        settings.CompanyId,
        settings.DefaultIncomeAccountName,
        settings.PrimaryColor,
        settings.NeutralColor,
        settings.ThemeMode,
        settings.PackSizeMap,
        settings.AccountFilters
    );
```

- [ ] **Step 3: Update UpdateCompanySettingsRequest**

In `backend/Application/Requests/UpdateCompanySettingsRequest.cs`, add the new fields to the record:

```csharp
public record UpdateCompanySettingsRequest(
    string DefaultIncomeAccountName,
    PrimaryColor? PrimaryColor = null,
    NeutralColor? NeutralColor = null,
    ThemeMode? ThemeMode = null,
    string? PackSizeMap = null,
    string? AccountFilters = null
);
```

- [ ] **Step 4: Update CompanyEndpoints PUT mapping**

In `backend/Endpoints/Endpoints/CompanyEndpoints.cs`, update the PUT handler to pass the new fields:

```csharp
app.MapPut("/api/companies/{companyId:guid}/settings", async (Guid companyId, UpdateCompanySettingsRequest request, IMediator mediator) =>
    {
        var command = new UpdateCompanySettingsCommand(
            companyId,
            request.DefaultIncomeAccountName,
            request.PrimaryColor,
            request.NeutralColor,
            request.ThemeMode,
            request.PackSizeMap,
            request.AccountFilters
        );
        await mediator.Send(command);
        return Results.NoContent();
    })
```

- [ ] **Step 5: Build backend to verify**

Run: `dotnet build backend/Host/Host.csproj`
Expected: Build succeeds with no errors.

---

### Task 4: Frontend Type Updates

**Files:**
- Modify: `frontend/src/types/Expenses.ts`
- Modify: `frontend/src/services/api/schema.ts`

- [ ] **Step 1: Update CompanySettings interface**

In `frontend/src/types/Expenses.ts`, add the two new fields to the `CompanySettings` interface:

```typescript
export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
  primaryColor?: PrimaryColor | null;
  neutralColor?: NeutralColor | null;
  themeMode?: ThemeMode | null;
  packSizeMap?: string | null;
  accountFilters?: string | null;
}
```

- [ ] **Step 2: Update OpenAPI schema**

In `frontend/src/services/api/schema.ts`, update `CompanySettingsResponse`:

```typescript
CompanySettingsResponse: {
    /** Format: uuid */
    id: string;
    /** Format: uuid */
    companyId: string;
    defaultIncomeAccountName: string;
    primaryColor?: PrimaryColor | null;
    neutralColor?: NeutralColor | null;
    themeMode?: ThemeMode | null;
    packSizeMap?: string | null;
    accountFilters?: string | null;
};
```

And update `UpdateCompanySettingsRequest`:

```typescript
UpdateCompanySettingsRequest: {
    defaultIncomeAccountName: string;
    primaryColor?: PrimaryColor | null;
    neutralColor?: NeutralColor | null;
    themeMode?: ThemeMode | null;
    packSizeMap?: string | null;
    accountFilters?: string | null;
};
```

---

### Task 5: Frontend Endpoint Path Migration

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts`

- [ ] **Step 1: Update all 19 endpoint paths**

In `frontend/src/services/ErpNextService.ts`, replace each old path with the new namespaced path:

| Line | Method | Old Path | New Path |
|------|--------|----------|----------|
| 107 | `getSalesSummary` | `/api/v2/method/grouped_sales_summary` | `/api/method/awesome_dashboard.api.dashboard.grouped_sales_summary` |
| 128 | `getStockValueSummary` | `/api/v2/method/get_average_stock_value` | `/api/method/awesome_dashboard.api.stock.get_average_stock_value` |
| 148 | `getDailyStockValueSummary` | `/api/v2/method/get_daily_stock_value` | `/api/method/awesome_dashboard.api.stock.get_daily_stock_value` |
| 167 | `getStockLevels` | `/api/v2/method/get_stock_levels` | `/api/method/awesome_dashboard.api.stock.get_stock_levels` |
| 182 | `getPrevGroupedExpenses` | `/api/v2/method/grouped_expenses_summary` | `/api/method/awesome_dashboard.api.dashboard.grouped_expenses_summary` |
| 202 | `getOrderBreakdown` | `/api/v2/method/dashboard_order_breakdown` | `/api/method/awesome_dashboard.api.dashboard.dashboard_order_breakdown` |
| 216 | `getExpenseBreakdown` | `/api/v2/method/dashboard_expense_breakdown` | `/api/method/awesome_dashboard.api.dashboard.dashboard_expense_breakdown` |
| 230 | `getAllAccounts` | `/api/v2/method/account_names` | `/api/method/awesome_dashboard.api.finance.account_names` |
| 269 | `getDashboardComplete` | `/api/v2/method/dashboard_complete` | `/api/method/awesome_dashboard.api.dashboard.dashboard_complete` |
| 288 | `getDashboardBarChart` | `/api/v2/method/dashboard_bar_chart` | `/api/method/awesome_dashboard.api.dashboard.dashboard_bar_chart` |
| 304 | `getDashboardSalesAggregated` | `/api/v2/method/dashboard_sales_aggregated` | `/api/method/awesome_dashboard.api.dashboard.dashboard_sales_aggregated` |
| 319 | `getDashboardPaymentEntries` | `/api/v2/method/dashboard_payment_entries` | `/api/method/awesome_dashboard.api.dashboard.dashboard_payment_entries` |
| 398 | `searchItems` | `/api/v2/method/search_items` | `/api/method/awesome_dashboard.api.item.search_items` |
| 407 | `createItem` | `/api/v2/method/create_item` | `/api/method/awesome_dashboard.api.item.create_item` |
| 428 | `getWarehouses` | `/api/v2/method/search_warehouses` | `/api/method/awesome_dashboard.api.lookup.search_warehouses` |
| 437 | `createFullPurchase` | `/api/v2/method/create_full_purchase` | `/api/method/awesome_dashboard.api.purchase.create_full_purchase` |
| 451 | `cancelFullPurchase` | `/api/v2/method/cancel_full_purchase` | `/api/method/awesome_dashboard.api.purchase.cancel_full_purchase` |
| 487 | `amendExpenseJournalEntry` | `/api/v2/method/amend_expense_journal_entry` | `/api/method/awesome_dashboard.api.finance.amend_expense_journal_entry` |
| 506 | `amendFullPurchase` | `/api/v2/method/amend_full_purchase` | `/api/method/awesome_dashboard.api.purchase.amend_full_purchase` |

Do NOT change the following methods (they use Frappe's `/api/resource/` REST API, which is unchanged):
- `getCompanyLogo` (line 88)
- `submitExpenseJournalEntry` (lines 354, 359)
- `searchSuppliers` (line 375)
- `createSupplier` (line 389)
- `getItemGroups` (line 419)
- `cancelExpenseJournalEntry` (line 460)
- `getJournalEntry` (line 471)
- `getPurchaseInvoice` (line 521)

- [ ] **Step 2: Verify frontend compiles**

Run: `npm run build` in `frontend/`
Expected: Build succeeds with no TypeScript errors.

---

### Task 6: Frontend Settings Wiring

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts`
- Modify: `frontend/src/stores/DataStore.ts`

- [ ] **Step 1: Add settings properties to ErpNextService**

In `frontend/src/services/ErpNextService.ts`, add two public properties to the class (right after the `private instance` declaration on line 69):

```typescript
export class ErpNextService {
  private instance: Axios;
  public packSizeMap?: string | null;
  public accountFilters?: string | null;
```

- [ ] **Step 2: Update getStockLevels to pass pack_size_map**

In the `getStockLevels()` method (line ~167), update the params to conditionally include `pack_size_map`:

```typescript
public getStockLevels() {
    const authStore = useAuthStore();

    const params: Record<string, unknown> = {
      company: authStore.company,
      warehouse: "Stores",
    };

    if (this.packSizeMap) {
      try {
        params.pack_size_map = JSON.parse(this.packSizeMap);
      } catch { /* ignore invalid JSON */ }
    }

    return this.instance
      .get<ErpNextResponse<StockDetail>>("/api/method/awesome_dashboard.api.stock.get_stock_levels", {
        params,
      })
      .then((resp) => resp?.data.data);
}
```

- [ ] **Step 3: Update getAllAccounts to filter client-side**

In the `getAllAccounts()` method (line ~226), add account filtering after fetching:

```typescript
public async getAllAccounts() {
    const authStore = useAuthStore();

    const accounts = await this.instance
      .get<AllAccountsResponse>("/api/method/awesome_dashboard.api.finance.account_names", {
        params: {
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);

    if (this.accountFilters && accounts) {
      let excludeList: string[] = [];
      try {
        excludeList = JSON.parse(this.accountFilters);
      } catch { /* ignore invalid JSON */ }

      if (excludeList.length > 0) {
        const excludeSet = new Set(excludeList.map((a) => a.toLowerCase()));
        accounts.expense = accounts.expense.filter(
          (a) => !excludeSet.has(a.account_name.toLowerCase()),
        );
        accounts.income = accounts.income.filter(
          (a) => !excludeSet.has(a.account_name.toLowerCase()),
        );
      }
    }

    return accounts;
}
```

- [ ] **Step 4: Pass settings from DataStore to ErpNextService**

In `frontend/src/stores/DataStore.ts`, in the `getData()` function (line ~34), set the properties on the service before calling `fetchAllData`:

```typescript
async function getData() {
    const erpNextService = new ErpNextService();
    const authStore = await import("@/stores/AuthStore").then((m) => m.useAuthStore());
    const companyId = authStore.companies?.find((c) => c.name === authStore.company)?.id;
    const settings = companyId ? await getCompanySettings(companyId) : null;
    erpNextService.packSizeMap = settings?.packSizeMap;
    erpNextService.accountFilters = settings?.accountFilters;
    const result = await fetchAllData(currentPeriod.value, erpNextService, settings?.defaultIncomeAccountName ?? "");
    
    await clear();
    // ... rest unchanged
```

Add the missing import for `CompanySettings` at the top of `DataStore.ts`. The `CompanySettings` type is already imported (line 6: `import type { Expense, Payment, CompanyExpenseMapping, CompanySettings, AccountMappings } from "../types/Expenses"`) — verify it's present.

- [ ] **Step 5: Verify frontend compiles**

Run: `npm run build` in `frontend/`
Expected: Build succeeds with no TypeScript errors.

---

### Task 7: Cleanup

**Files:**
- Delete: `erpnext/` directory (all contents)
- Modify: `README.md`

- [ ] **Step 1: Remove the erpnext scripts directory**

Run:
```bash
rm -rf erpnext/
```
Expected: `erpnext/` directory no longer exists.

- [ ] **Step 2: Add awesome_dashboard link to README**

In `README.md`, find the section about ERPNext integration scripts and add a note:

```markdown
## ERPNext Integration

This dashboard consumes endpoints provided by the [awesome_dashboard](https://github.com/Stelele/awesome_dashboard_scripts) Frappe app. Install it on your ERPNext site via:

```bash
bench get-app https://github.com/Stelele/awesome_dashboard_scripts
bench --site your-site install-app awesome_dashboard
```

The app provides all server-side endpoints used for sales, purchases, expenses, stock, and journal entry operations.
```

Remove any existing references to the `erpnext/` directory or manual script installation.

---

### Task 8: Build Verification

**Files:** (none — verification only)

- [ ] **Step 1: Build backend**

Run: `dotnet build backend/Host/Host.csproj`
Expected: Build succeeds.

- [ ] **Step 2: Run backend tests**

Run: `dotnet test backend/Tests/Tests.csproj`
Expected: All tests pass.

- [ ] **Step 3: Build frontend**

Run: `npm run build` in `frontend/`
Expected: Build succeeds.

- [ ] **Step 4: Type-check frontend**

Run: `npm run typecheck` in `frontend/` (if script exists, check package.json)
Expected: No type errors.
