# Company Features Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Per-company page visibility driven by Feature/Recipe domain entities, gated in nav, routes, the sync worker and feature-scoped API endpoints.

**Architecture:** Four new entities (Feature, Recipe, RecipeFeature, CompanyRecipe) with live many-to-many recipe composition; effective features = union of a company's recipes. Effective codes ride on `GET /companies`; frontend filters nav/routes from them; a minimal-API endpoint filter gates company-scoped feature endpoints. Management is endpoint-only (no admin UI).

**Tech Stack:** .NET 10, EF Core (SQLite at runtime — follow the code, not AGENTS.md's PostgreSQL note), MediatR CQRS + FluentValidation, xUnit integration tests (in-memory SQLite via `IntegrationTestFactory`); Vue 3 + Pinia + vue-router + @nuxt/ui, openapi-fetch with generated schema.

**Spec:** `docs/superpowers/specs/2026-11-17-company-features-design.md`

## Global Constraints

- Feature codes: lowercase, regex `^[a-z][a-z0-9-]*$`, max 50 chars, unique, **immutable after creation** (stable key across layers). Seeded codes exactly: `sales`, `stock`, `expenses`.
- Effective features = union across all company's recipes; recipes live (edits propagate); no per-company overrides; no recipes → no gated pages.
- Overview and auth pages (`/login`, `/reset-password`) are never gated.
- Unknown entity ids in command payloads → `400` (throw FluentValidation `ValidationException`); missing entity → `404` (`NotFoundException`); duplicate code/name → `409` (`DuplicateDomainMemberException`); delete of referenced feature/recipe → `409` (new `ResourceInUseException`).
- Every mutating feature/recipe/assignment command carries `[InvalidateCache(Category = "companies")]` AND `[InvalidateCache(Category = "company")]` (both cached queries return `CompanyResponse`, which embeds features).
- Soft delete via `IsDeleted` + `HasQueryFilter` for Feature and Recipe (ExpenseType convention). Join rows (RecipeFeature, CompanyRecipe) hard-delete.
- Guid columns stored as lowercase strings (`HasConversion` pattern from `ExpenseTypeEntity`).
- New permissions: `read:features`, `manage:features` — Admin-only; effective-features and company-recipes reads use existing `read:companies`, writes use `update:companies`.
- Backend tests: xUnit, `IClassFixture<IntegrationTestFactory>`, `ResetDatabaseAsync()` at test start, wire JSON via `TestJson.Options`. Frontend has no unit test framework — verification is `npm run build:test` (vue-tsc + vite) plus real-browser smoke (Task 9).
- Work happens on branch `feat/company-features`. Commit after every task.

## Review Focus

1. **Hard-refresh deep link before companies load** — guard must await company load, not bounce a valid page to `/`. Pinned: Task 7 step 1 (ensureLoaded) + Task 9 smoke.
2. **Stale Dexie companies cache without `features`** (pre-upgrade rows) — nav must recover via the worker's immediate startup sync, not require re-login. Pinned: Task 9 smoke.
3. **Recipe edit vs cached `GET /companies`** — invalidation must make the change visible immediately on the API. Pinned: Task 5 test.
4. **Deleting a company that holds recipes** — FK cascade must not throw. Pinned: Task 4 test.
5. **Duplicate recipe ids in one PUT** — dedupe, no unique-index 500. Pinned: Task 4 test.

---

### Task 1: Domain entities, EF configuration, migration, seed data

**Files:**
- Create: `backend/Domain/Features/Feature.cs`, `Recipe.cs`, `RecipeFeature.cs`, `CompanyRecipe.cs`
- Create: `backend/Infrastructure/Models/FeatureEntity.cs`, `RecipeEntity.cs`, `RecipeFeatureEntity.cs`, `CompanyRecipeEntity.cs` (these are `IEntityTypeConfiguration<T>` classes, per existing naming)
- Modify: `backend/Infrastructure/Models/DashboardDbContext.cs` (4 DbSets + 4 Configure calls)
- Modify: `backend/Tests/IntegrationTestFactory.cs` (`ResetDatabaseAsync`: `db.CompanyRecipes.RemoveRange(db.CompanyRecipes);` before the Companies removal)
- Delete: empty vestigial dirs `backend/Domain/Pages`, `backend/Domain/UserCompanyPages` (spec: superseded, never committed)
- Test: `backend/Tests/FeaturesTests.cs`
- Generated: `backend/Infrastructure/Migrations/*_AddFeaturesAndRecipes.cs`

**Interfaces:**
- Produces: `Domain.Features.Feature { Code, Name, Description, IsDeleted }`, `Recipe { Name, Description, IsDeleted }`, `RecipeFeature { RecipeId, FeatureId, Recipe, Feature }`, `CompanyRecipe { CompanyId, RecipeId, Company, Recipe }` — all `: Base`. DbSets: `db.Features`, `db.Recipes`, `db.RecipeFeatures`, `db.CompanyRecipes`.
- Seed ids (fixed, HasData): features `…0002-000000000001` sales, `…02` stock, `…03` expenses (prefix `00000000-0000-0000-`); recipe `00000000-0000-0000-0002-000000000100` "Default"; RecipeFeature rows `…0200/0201/0202` linking Default→each feature. Use a fixed `seedDate` like `ExpenseTypeEntity`.

- [ ] **Step 1: Write the failing test** — `FeaturesTests.SeedData_ExistsAfterMigration`: resolve `DashboardDbContext` from `_factory.Services` scope; assert `db.Features.Count() == 3` with codes `sales`, `stock`, `expenses`; `db.Recipes` contains "Default"; `db.RecipeFeatures.Count() == 3`.
- [ ] **Step 2: Run** `dotnet test backend/Tests --filter SeedData_ExistsAfterMigration` → FAIL (DbSet missing / compile error).
- [ ] **Step 3: Implement** entities, configs, DbContext wiring. Config details: table names `Features`/`Recipes`/`RecipeFeatures`/`CompanyRecipes`; Guid lowercase-string conversion; unique indexes on `Feature.Code`, `Recipe.Name`, `(RecipeId, FeatureId)`, `(CompanyId, RecipeId)`; query filters `!IsDeleted` on Feature/Recipe; FKs: Company→CompanyRecipe **Cascade**, Recipe→CompanyRecipe **Restrict**, Recipe→RecipeFeature Cascade, Feature→RecipeFeature Restrict; HasData seeds above. Add the reset line.
- [ ] **Step 4: Create migration** — run from `backend/`: `dotnet ef migrations add AddFeaturesAndRecipes --project Infrastructure --startup-project Host`.
- [ ] **Step 5: Run** the filter test → PASS. Then `dotnet test backend/Tests` → all existing tests PASS (factory applies the migration).
- [ ] **Step 6: Commit** — `feat: feature/recipe domain entities, persistence and seed data`

### Task 2: Feature registry CRUD

**Files:**
- Create: `backend/Application/Features/GetFeaturesQuery.cs`, `CreateFeatureCommand.cs` (record + validator + handler in one file, ExpenseTypes style), `UpdateFeatureCommand.cs`, `DeleteFeatureCommand.cs`
- Create: `backend/Application/DTOs/FeatureResponse.cs`, `backend/Application/Requests/FeatureRequests.cs` (CreateFeatureRequest, UpdateFeatureRequest)
- Create: `backend/Endpoints/Endpoints/FeatureEndpoints.cs` (features routes only for now)
- Modify: `backend/Domain/Exceptions/Exceptions.cs` (add `ResourceInUseException(string message) : Exception(message)`), `backend/Host/Middleware/GlobalExceptionMiddleware.cs` (arm → 409, copy the DuplicateDomainMemberException arm shape), `backend/Endpoints/Permissions.cs`, `backend/Endpoints/DependancyInjection.cs` (`.AddPermission` ×2 + `.MapFeatureEndpoints()` in `MapApi`), `backend/Endpoints/Tags.cs` (`Features = "Features"`), `backend/Endpoints/Authentication/RolePermissions.cs` (both into `AllPermissions`), `backend/Tests/TestAuthHandler.cs` (append `read:features manage:features` to scope claim), `backend/Tests/IntegrationTestFactory.cs` (both into permissions array)
- Test: `backend/Tests/FeaturesTests.cs` (extend)

**Interfaces:**
- Consumes: Task 1 entities/DbSets.
- Produces: `FeatureResponse(Guid Id, string Code, string Name, string Description)` + `FromDomain(Feature)`; `GetFeaturesQuery : IQuery<List<FeatureResponse>>`; `CreateFeatureCommand(string Code, string Name, string Description) : ICommand<Guid>`; `UpdateFeatureCommand(Guid Id, string Name, string Description) : ICommand` (Code immutable); `DeleteFeatureCommand(Guid Id) : ICommand`; `Permissions.ReadFeatures = "read:features"`, `Permissions.ManageFeatures = "manage:features"`; routes `GET/POST /features`, `PUT/DELETE /features/{id:guid}`.

- [ ] **Step 1: Write failing tests** in `FeaturesTests`: `CreateFeature_Returns201_AndAppearsInList`; `CreateFeature_DuplicateCode_Returns409` (seeded code `sales`); `CreateFeature_InvalidCode_Returns400` (`"Stock Page"`); `UpdateFeature_ChangesNameNotCode`; `DeleteFeature_Unreferenced_Returns204_AndGoneFromList` (create a throwaway feature first). Local private helpers `CreateSiteAsync`/`CreateCompanyAsync` copied from `CompaniesTests` pattern where needed.
- [ ] **Step 2: Run** `dotnet test backend/Tests --filter FeaturesTests` → FAIL (404 routes).
- [ ] **Step 3: Implement** commands/validators/handlers/DTO/requests/endpoints/permission wiring. Validator: Code `NotEmpty().MaximumLength(50).Matches("^[a-z][a-z0-9-]*$")`, Name `NotEmpty().MaximumLength(100)`, Description `MaximumLength(255)`. Duplicate code check uses `IgnoreQueryFilters()` (ExpenseType pattern). Delete: if `db.RecipeFeatures.AnyAsync(rf => rf.FeatureId == id)` throw `ResourceInUseException`; else `IsDeleted = true`. All three mutating commands carry both InvalidateCache attributes. GET requires `ReadFeatures`, writes `ManageFeatures`.
- [ ] **Step 4: Run** filter → PASS; full `dotnet test backend/Tests` → PASS.
- [ ] **Step 5: Commit** — `feat: feature registry CRUD endpoints`

### Task 3: Recipe CRUD

**Files:**
- Create: `backend/Application/Features/GetRecipesQuery.cs`, `CreateRecipeCommand.cs`, `UpdateRecipeCommand.cs`, `DeleteRecipeCommand.cs`, `GetCompanyRecipesQuery.cs`
- Create: `backend/Application/DTOs/RecipeResponse.cs`, `backend/Application/Requests/RecipeRequests.cs` (CreateRecipeRequest, UpdateRecipeRequest)
- Modify: `backend/Endpoints/Endpoints/FeatureEndpoints.cs` (recipes routes)
- Test: `backend/Tests/RecipesTests.cs`

**Interfaces:**
- Consumes: Task 2 `FeatureResponse`, permissions, exception types.
- Produces: `RecipeResponse(Guid Id, string Name, string Description, List<FeatureResponse> Features)`; `CreateRecipeCommand(string Name, string Description, Guid[] FeatureIds) : ICommand<Guid>`; `UpdateRecipeCommand(Guid Id, string Name, string Description, Guid[] FeatureIds) : ICommand` (replaces feature set); `DeleteRecipeCommand(Guid Id) : ICommand`; `GetRecipesQuery : IQuery<List<RecipeResponse>>`; `GetCompanyRecipesQuery(Guid CompanyId) : IQuery<List<RecipeResponse>>`; routes `GET/POST /recipes`, `PUT/DELETE /recipes/{id:guid}`.

- [ ] **Step 1: Write failing tests** in `RecipesTests`: `CreateRecipe_WithSeededFeatureIds_Returns201_AndListIncludesFeatures` (assert response Features codes contain `sales`); `CreateRecipe_UnknownFeatureId_Returns400`; `UpdateRecipe_ReplacesFeatureSet` (create with [sales], update to [stock, expenses], assert codes exactly); `DeleteFeature_ReferencedByRecipe_Returns409`; `DeleteRecipe_Unreferenced_Returns204`. List assertions use containment, not exact counts (shared fixture, seeded + test-created recipes coexist).
- [ ] **Step 2: Run** `dotnet test backend/Tests --filter RecipesTests` → FAIL.
- [ ] **Step 3: Implement.** Unknown FeatureIds → `ValidationException` listing the ids (400). Duplicate recipe name → `DuplicateDomainMemberException`. Delete recipe: referenced by `db.CompanyRecipes` → `ResourceInUseException`; else soft-delete. Update replaces RecipeFeature rows (remove missing, add new). Both InvalidateCache attributes on all mutating commands. GET `ReadFeatures`, writes `ManageFeatures`.
- [ ] **Step 4: Run** filter → PASS; full suite → PASS.
- [ ] **Step 5: Commit** — `feat: recipe CRUD endpoints`

### Task 4: Company assignments, effective-features resolver, creation input, seeder

**Files:**
- Create: `backend/Application/Features/CompanyFeaturesResolver.cs`, `GetCompanyFeaturesQuery.cs`, `SetCompanyRecipesCommand.cs`
- Create: `backend/Infrastructure/Persistence/FeatureSeeder.cs`
- Modify: `backend/Application/Requests/CreateCompanyRequest.cs` (append `Guid[]? RecipeIds = null`), `backend/Application/Companies/CreateCompanyCommand.cs` (same param) + `CreateCompanyCommandHandler.cs` (validate ids exist → ValidationException; insert CompanyRecipe rows), `backend/Application/Requests/RecipeRequests.cs` (add `SetCompanyRecipesRequest(Guid[] RecipeIds)`), `backend/Endpoints/Endpoints/FeatureEndpoints.cs` (company routes), `backend/Infrastructure/DependancyInjection.cs` (call `FeatureSeeder.SeedAsync` after AdminSeeder, same try/catch-log shape)
- Test: `backend/Tests/CompanyFeaturesTests.cs`

**Interfaces:**
- Consumes: Tasks 1–3 entities, commands, exceptions.
- Produces:
```csharp
public static class CompanyFeaturesResolver
{
    public static Task<List<string>> GetEffectiveCodesAsync(DashboardDbContext db, Guid companyId, CancellationToken ct);
    public static Task<Dictionary<Guid, List<string>>> GetEffectiveCodesAsync(DashboardDbContext db, IReadOnlyCollection<Guid> companyIds, CancellationToken ct);
}
```
`GetCompanyFeaturesQuery(Guid CompanyId) : IQuery<List<string>>`; `SetCompanyRecipesCommand(Guid CompanyId, Guid[] RecipeIds) : ICommand`; routes `GET /companies/{companyId:guid}/features` (read:companies) → `List<string>`, `GET /companies/{companyId:guid}/recipes` (read:companies) → `List<RecipeResponse>`, `PUT /companies/{companyId:guid}/recipes` (update:companies, body SetCompanyRecipesRequest) → 204; `FeatureSeeder.SeedAsync(DashboardDbContext db, CancellationToken ct = default)`.

- [ ] **Step 1: Write failing tests** in `CompanyFeaturesTests`:
  - `Union_AcrossTwoRecipes` — company with recipe A [sales] + recipe B [stock] → features exactly {sales, stock}.
  - `LiveRecipeEdit_Propagates` — assign recipe, then UpdateRecipeCommand adds expenses → GET features includes expenses.
  - `SetCompanyRecipes_ReplacesSet_AndDedupes` — PUT [A, A, B] → 204, one row per recipe, features = union; then PUT [] → features empty.
  - `SetCompanyRecipes_UnknownRecipeId_Returns400`.
  - `CreateCompany_WithRecipeIds_AssignsThem` — POST /companies with recipeIds → GET features matches; without → empty.
  - `DeleteCompany_WithRecipes_Returns204` (Review Focus 4).
  - `Seeder_AssignsDefault_ToCompaniesWithoutRecipes` — create company with no recipes, call `FeatureSeeder.SeedAsync` via factory scope, assert features = {sales, stock, expenses}; company WITH recipes untouched.
- [ ] **Step 2: Run** `dotnet test backend/Tests --filter CompanyFeaturesTests` → FAIL.
- [ ] **Step 3: Implement.** Resolver: one join query CompanyRecipes→RecipeFeatures→Features (query filters exclude soft-deleted), `Distinct()` codes; bulk overload groups by CompanyId, missing companies → empty list. SetCompanyRecipes: dedupe ids, validate all exist (ValidationException), remove existing rows, insert. Seeder: find Default recipe by fixed seed id; for each company with no CompanyRecipe rows, add one. Both InvalidateCache attributes on SetCompanyRecipesCommand.
- [ ] **Step 4: Run** filter → PASS; full suite → PASS.
- [ ] **Step 5: Commit** — `feat: company recipe assignments, effective features, creation input and seeder`

### Task 5: Effective features on CompanyResponse

**Files:**
- Modify: `backend/Application/DTOs/CompanyResponse.cs` (append `string[] Features`; `FromDomain(Company company, IEnumerable<string> features)`), `backend/Application/Companies/GetCompaniesQueryHandler.cs`, `GetCompanyByIdQueryHandler.cs` (bulk resolver / single resolver)
- Test: `backend/Tests/CompanyFeaturesTests.cs` (extend)

**Interfaces:**
- Consumes: Task 4 `CompanyFeaturesResolver`.
- Produces: `CompanyResponse(Id, Name, Description, SiteId, Features)` — the contract the frontend consumes.

- [ ] **Step 1: Write failing tests:** `GetCompanies_IncludesEffectiveFeatures` (company with recipes → response.Features codes match); `GetCompanyById_IncludesEffectiveFeatures`; `RecipeEdit_InvalidatesCompaniesCache` (Review Focus 3): GET /companies → edit recipe via PUT /recipes/{id} → GET /companies again → new code present immediately (no 5-min wait).
- [ ] **Step 2: Run** filter → FAIL (missing property / stale cache).
- [ ] **Step 3: Implement** DTO + both handlers via resolver; fix any other `FromDomain` call sites the compiler surfaces.
- [ ] **Step 4: Run** filter → PASS; full suite → PASS.
- [ ] **Step 5: Commit** — `feat: embed effective features in company responses`

### Task 6: Backend feature gate on expense-mappings

**Files:**
- Create: `backend/Endpoints/FeatureGateExtensions.cs`
- Modify: `backend/Endpoints/Endpoints/ExpenseEndpoints.cs` (`.RequireFeature("expenses")` on GET and PUT `/companies/{companyId:guid}/expense-mappings`)
- Test: `backend/Tests/FeatureGateTests.cs`

**Interfaces:**
- Consumes: Task 4 resolver.
- Produces: `public static RouteHandlerBuilder RequireFeature(this RouteHandlerBuilder builder, string featureCode)` — endpoint filter: parse `companyId` from `HttpContext.Request.RouteValues`, resolve `DashboardDbContext` from `RequestServices`, effective codes via resolver; missing code → `Results.NotFound()`, else `await next(context)`.

- [ ] **Step 1: Write failing tests:** `ExpenseMappings_CompanyWithoutExpenses_Returns404` (GET and PUT; company with recipe containing only sales); `ExpenseMappings_CompanyWithExpenses_Returns200` (GET; Default recipe).
- [ ] **Step 2: Run** `dotnet test backend/Tests --filter FeatureGateTests` → FAIL (200 instead of 404).
- [ ] **Step 3: Implement** filter + apply to the two routes.
- [ ] **Step 4: Run** filter → PASS; full suite → PASS (existing ExpenseTests companies get Default via seeder? No — tests reset DB and create companies via POST without recipes. If existing expense-mapping tests now 404, update those tests to assign the Default recipe at setup — that is the intended behavior change, not a regression).
- [ ] **Step 5: Commit** — `feat: gate expense-mappings endpoints on expenses feature`

### Task 7: Frontend — schema, FeatureStore, nav filter, route guard

**Files:**
- Generated: `frontend/src/services/api/schema.ts` (via codegen)
- Create: `frontend/src/stores/FeatureStore.ts`
- Modify: `frontend/src/stores/NavStore.ts`, `frontend/src/routes/index.ts`, `frontend/src/guards/auth.ts`

**Interfaces:**
- Consumes: Task 5 `CompanyResponse.features` (schema type `components["schemas"]["CompanyResponse"]["features"]: string[]`).
- Produces: `useFeatureStore()` → `{ features: ComputedRef<string[]>, hasFeature(code: string): boolean, ensureLoaded(): Promise<void> }`; route `meta.feature?: string`; nav items filtered.

- [ ] **Step 1: Regenerate schema** — start backend (`dotnet run --project backend/Host`, port 5165), then `cd frontend && npm run codegen`. Verify `CompanyResponse` has `features` in schema.ts. Stop backend.
- [ ] **Step 2: Implement FeatureStore** — `features` computed from `authStore.companies.find(c => c.name === authStore.company)?.features ?? []`; `ensureLoaded()`: if `authStore.accessToken && authStore.companies.length === 0`, set `authStore.companies = await CachedApiClient.getInstance().getUserCompanies()` (Review Focus 1: guard must not bounce valid pages on hard refresh).
- [ ] **Step 3: NavStore** — keep current items as a static registry with optional `feature` codes (`sales`, `stock`, `expenses`; Overview none); `navItems` becomes `computed` filtering registry by `featureStore.hasFeature`, stripping the `feature` key from emitted items. `SideBar.vue`/`DashboardLayout.vue` need no changes.
- [ ] **Step 4: Routes + guard** — add `meta: { feature: "sales" | "stock" | "expenses" }` to the three routes; `authGuard` becomes async: after the token check, if `to.meta.feature`, `await useFeatureStore().ensureLoaded()`; then `hasFeature(to.meta.feature as string)` — false → `{ path: "/" }`.
- [ ] **Step 5: Verify** — `cd frontend && npm run build:test` → PASS (vue-tsc clean).
- [ ] **Step 6: Commit** — `feat: feature-aware nav and route gating`

### Task 8: Frontend — feature-aware sync worker

**Files:**
- Modify: `frontend/src/services/cache/cacheSyncWorker.ts`

**Interfaces:**
- Consumes: Task 7 schema (`CompanyResponse.features`); worker already fetches `/companies` into `db.companies` — derive the company→features map from those rows, no protocol change.

- [ ] **Step 1: Restructure reference sync** — fetch `/companies` FIRST (await, store to db), then `Promise.allSettled` of users, sites, and expense-types; expense-types fetch only if `companies.some(c => c.features?.includes("expenses"))`. Keep SYNC_ERROR reporting shape.
- [ ] **Step 2: Per-company loop** — expense-mappings fetch only if `company.features?.includes("expenses")`; settings fetch unchanged. Progress totals reflect skipped fetches.
- [ ] **Step 3: Verify** — `npm run build:test` → PASS.
- [ ] **Step 4: Commit** — `feat: skip feature-scoped sync for companies without the feature`

### Task 9: End-to-end verification

**Files:** none created; fixes if smoke fails go to the owning task's files.

- [ ] **Step 1: Full backend suite** — `dotnet test backend/Tests` → all PASS.
- [ ] **Step 2: Start stack** — backend (`dotnet run --project backend/Host`) + frontend (`cd frontend && npm run dev`).
- [ ] **Step 3: Browser smoke via chrome-devtools MCP** (Review Focus 1, 2):
  - Log in as admin; company with Default recipe → nav shows Overview/Sales/Stock/Expenses; hard-refresh `/stock` → page renders (guard awaited load).
  - Via API (curl with session token): create recipe "Services" with [sales, expenses] only; create company assigned to it; switch to that company in the UI → Stock absent from nav; navigate `/stock` → redirected to `/`; no stock ERPNext calls in network log.
  - Reload with pre-existing Dexie cache (no re-login) → nav correct within seconds (worker's immediate startup sync refreshed companies).
  - Edit "Services" recipe via API to add stock → within worker refresh interval (or after reload) Stock appears; remove again → disappears.
- [ ] **Step 4: Final commit if fixes were needed**, then whole-branch review per the execution skill.