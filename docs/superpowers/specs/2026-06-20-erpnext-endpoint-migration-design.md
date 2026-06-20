# ERPNext Endpoint Migration Design

**Date**: 2026-06-20
**Status**: Approved

## Context

The dashboard currently calls 19 manually-entered ERPNext server scripts via `/api/v2/method/<script_name>` paths. These scripts live in the `erpnext/server_scripts/` and `erpnext/optimize/` directories and must be copy-pasted into each ERPNext site's Server Script doctype — a manual, non-scalable process.

The `awesome_dashboard` Frappe app (https://github.com/Stelele/awesome_dashboard_scripts) provides the same functionality but is installed once via `bench install-app awesome_dashboard` and injects all endpoints under the `awesome_dashboard.api.*` namespace.

Additionally, the `erpnext/njeremoto-overrides/` directory contained 2 scripts with hardcoded company-specific values (liquor pack-size map, expense/income account lists). These have been removed and must be replaced with per-company configurable settings.

## Goal

1. Replace all `/api/v2/method/<script_name>` calls with namespaced `awesome_dashboard.api.<module>.<function>` paths
2. Generalize njeremoto-specific behaviors into per-company CompanySettings fields
3. Remove the old `erpnext/` scripts directory
4. Add documentation linking to the awesome_dashboard repo

## Design

### Backend: CompanySettings Expansion

Add two new nullable fields to the `CompanySettings` domain entity:

| Field           | Type       | Purpose                                              | Example                               |
|-----------------|------------|------------------------------------------------------|---------------------------------------|
| `PackSizeMap`   | `string?`  | JSON dict mapping item types to units-per-crate      | `{"Pint": 24, "Quart": 12}`          |
| `AccountFilters`| `string?`  | JSON array of account names to exclude from listings | `["Canteen", "Spoiled Meat"]`        |

**Files to change:**

- `backend/Domain/CompanySettings/CompanySettings.cs` — add properties
- `backend/Infrastructure/Models/CompanySettingsEntity.cs` — EF configuration (nvarchar(max), nullable)
- `backend/Application/CompanySettings/UpdateCompanySettingsCommand.cs` — add to record and handler
- `backend/Application/CompanySettings/GetCompanySettingsQuery.cs` — no change needed (auto-maps)
- `backend/Application/DTOs/CompanySettingsResponse.cs` — add fields to DTO
- `backend/Application/Requests/UpdateCompanySettingsRequest.cs` — add fields and validation
- `backend/Endpoints/Endpoints/CompanyEndpoints.cs` — pass new fields through PUT mapping
- New EF migration: `AddPackSizeMapAndAccountFilters`

### Frontend: Endpoint Path Migration

Update 19 endpoint paths in `ErpNextService.ts`:

| Old Path                                          | New Path                                                     |
|---------------------------------------------------|--------------------------------------------------------------|
| `/api/v2/method/grouped_sales_summary`            | `/api/method/awesome_dashboard.api.dashboard.grouped_sales_summary` |
| `/api/v2/method/get_average_stock_value`          | `/api/method/awesome_dashboard.api.stock.get_average_stock_value` |
| `/api/v2/method/get_daily_stock_value`            | `/api/method/awesome_dashboard.api.stock.get_daily_stock_value` |
| `/api/v2/method/get_stock_levels`                 | `/api/method/awesome_dashboard.api.stock.get_stock_levels` |
| `/api/v2/method/grouped_expenses_summary`         | `/api/method/awesome_dashboard.api.dashboard.grouped_expenses_summary` |
| `/api/v2/method/dashboard_order_breakdown`        | `/api/method/awesome_dashboard.api.dashboard.dashboard_order_breakdown` |
| `/api/v2/method/dashboard_expense_breakdown`      | `/api/method/awesome_dashboard.api.dashboard.dashboard_expense_breakdown` |
| `/api/v2/method/account_names`                    | `/api/method/awesome_dashboard.api.finance.account_names` |
| `/api/v2/method/dashboard_complete`               | `/api/method/awesome_dashboard.api.dashboard.dashboard_complete` |
| `/api/v2/method/dashboard_bar_chart`              | `/api/method/awesome_dashboard.api.dashboard.dashboard_bar_chart` |
| `/api/v2/method/dashboard_sales_aggregated`       | `/api/method/awesome_dashboard.api.dashboard.dashboard_sales_aggregated` |
| `/api/v2/method/dashboard_payment_entries`        | `/api/method/awesome_dashboard.api.dashboard.dashboard_payment_entries` |
| `/api/v2/method/search_items`                     | `/api/method/awesome_dashboard.api.item.search_items` |
| `/api/v2/method/create_item`                      | `/api/method/awesome_dashboard.api.item.create_item` |
| `/api/v2/method/search_warehouses`                | `/api/method/awesome_dashboard.api.lookup.search_warehouses` |
| `/api/v2/method/create_full_purchase`             | `/api/method/awesome_dashboard.api.purchase.create_full_purchase` |
| `/api/v2/method/cancel_full_purchase`             | `/api/method/awesome_dashboard.api.purchase.cancel_full_purchase` |
| `/api/v2/method/amend_expense_journal_entry`      | `/api/method/awesome_dashboard.api.finance.amend_expense_journal_entry` |
| `/api/v2/method/amend_full_purchase`              | `/api/method/awesome_dashboard.api.purchase.amend_full_purchase` |

Standard Frappe REST API calls (`/api/resource/Journal Entry`, `/api/resource/Supplier`, `/api/resource/Item Group`, `/api/resource/Purchase Invoice`) remain unchanged.

### Frontend: Settings Wiring

**PackSizeMap** — `ErpNextService.getStockLevels()` reads `packSizeMap` from `CompanySettings` (via `DataStore`) and passes it as `pack_size_map` parameter. The awesome_dashboard `get_stock_levels` endpoint already accepts this optional parameter.

**AccountFilters** — `ErpNextService.getAllAccounts()` reads `accountFilters` from `CompanySettings` and filters returned expense/income lists client-side before they are consumed by `getAccountMappings()`.

**Types/schema:**
- `frontend/src/types/Expenses.ts` — add `packSizeMap?: string | null` and `accountFilters?: string | null` to `CompanySettings` interface
- `frontend/src/services/api/schema.ts` — add fields to `CompanySettingsResponse` and `UpdateCompanySettingsRequest`

### Cleanup

- Remove entire `erpnext/` directory (server_scripts, optimize, njeremoto-overrides)
- Add note in `README.md` linking to https://github.com/Stelele/awesome_dashboard_scripts with a brief explanation that the ERPNext endpoints now live in that Frappe app

### Settings Flow (End-to-End)

```
Admins set per-company settings via dashboard UI
        |
        v
PUT /api/companies/{companyId}/settings  (packSizeMap, accountFilters)
        |
        v
Stored in SQLite CompanySettings table
        |
        v
Frontend reads via GET /api/companies/{companyId}/settings
(cached in IndexedDB via CachedApiClient / cacheSyncWorker)
        |
        v
ErpNextService uses settings when calling awesome_dashboard endpoints:
  - getStockLevels() passes PackSizeMap -> awesome_dashboard.api.stock.get_stock_levels
  - getAllAccounts() filters response client-side using AccountFilters
```

### Not in Scope

- No toggle between old scripts and new app endpoints — the awesome_dashboard app fully replaces the old scripts
- No backend proxy layer — frontend continues calling ERPNext directly
- No changes to the `awesome_dashboard` Python app itself — it already supports the needed parameterization
