# Company Features — Design

**Date:** 2026-11-17 · **Status:** draft for approval · **Repo:** erpnext-dashboard

## Intent

Pages should exist for a company only when they make sense for that business.
A refunds page is clutter for a company that never refunds; Livestock is
meaningless for a fashion company; Stock is noise for a services company.
There is a second, harder reason: **ERPNext apps are only installed where the
feature is needed**, so calls for an unassigned feature would hit missing
apps and fail. Gating is therefore functional protection, not just UI tidiness.

"Feature = page" is the agreed approximation: frontend views are the units of
functionality.

## Goals / Non-goals / Anti-goals

| | |
|---|---|
| **Goals** | Feature as a domain entity · recipes as live, composable bundles · per-company effective features = union of assigned recipes · nav + routes gated · feature-scoped API calls gated (both .NET endpoints and browser→ERPNext calls) · management via endpoints, existing pattern |
| **Non-goals** | Admin UI (endpoints only, for now) · billing/licensing · per-user page permissions · per-company feature overrides (composition replaces them) · gating ERPNext itself |
| **Anti-goals** | A company silently losing pages it should have after a recipe edit without anyone noticing · a plugin/dynamic-page system · breaking existing companies at rollout |

## Domain model

```
Feature ◀── RecipeFeature ──▶ Recipe ◀── CompanyRecipe ──▶ Company
                                                              │
effective features(company) = union of features over all its recipes
```

| Entity | Fields (beyond `Base`) | Notes |
|---|---|---|
| `Feature` | `Code` (unique, stable, lowercase — e.g. `stock`), `Name`, `Description` | The shared key across frontend routes, nav and backend gates |
| `Recipe` | `Name`, `Description` | Named bundle, e.g. "Fashion Retail", "Refunds", "Livestock Farm" |
| `RecipeFeature` | `RecipeId`, `FeatureId` | join |
| `CompanyRecipe` | `CompanyId`, `RecipeId` | join, many-to-many |

Semantics (all decided in conversation):

- **Live:** editing a recipe updates every company holding it. Removing a
  feature from a recipe hides it for all of them — recipe edits are bulk ops.
- **Composable:** a company holds many recipes. One fashion company that wants
  refunds = "Fashion Retail" + "Refunds". No per-company overrides exist.
- **No recipes → no gated pages** (see Overview call below). No implicit
  everything-on fallback.

## Key finding: where ERPNext calls actually happen

```
browser ──ErpNextService (axios, site url + token)──▶ ERPNext   ← data calls
browser ──openapi-fetch──▶ .NET backend                        ← metadata/settings
.NET backend ──HTTP──▶ ERPNext                                 ← logo fetch only
```

The .NET backend does **not** proxy ERPNext data. So "API gating" has two
surfaces, and both are in scope:

**Scope nuance (user):** the `awesome_dashboard` Frappe app is installed on
every site — its endpoints (sales/stock/expense aggregates used by Overview
and the current pages) are always available. The missing-app failure mode
applies to **feature-specific apps** (e.g. a future livestock app). Gates
protect those; they are not needed for always-available core endpoints.

1. **.NET endpoints** — feature-scoped, company-scoped endpoints check the
   company's effective features before doing work.
2. **Browser→ERPNext calls** — gated by the page never mounting (nav + route
   guard) **and** by feature-aware background sync. The global cache-sync
   worker (`App.vue → useCacheSync → cacheSyncWorker`) fetches expense-types
   and per-company expense-mappings regardless of page; it must skip
   feature-scoped fetches when the feature is off. This is the leak point a
   nav-only gate would miss.

## API surface (new endpoints, existing MediatR/minimal-API pattern)

| Endpoint | Purpose | Auth |
|---|---|---|
| `GET /features` · `POST /features` · `PUT /features/{id}` · `DELETE /features/{id}` | manage feature registry | `manage:features` (read: `read:features`) |
| `GET /recipes` · `POST /recipes` · `PUT /recipes/{id}` · `DELETE /recipes/{id}` | manage recipes; PUT replaces the feature set | `manage:features` |
| `GET /companies/{id}/recipes` · `PUT /companies/{id}/recipes` | read / replace a company's recipe set | read: `read:companies` · write: `update:companies` |
| `GET /companies/{id}/features` | effective feature codes (union) | `read:companies` |

Existing endpoint change: `POST /companies` accepts `recipeIds` in the
request and assigns them during creation (user decision).

- `GET /companies` response gains `features: string[]` per company (effective
  union). This is the frontend's single source — one request on login covers
  nav, guards and the sync worker for **all** companies, no per-company fetch
  loop, no 404-as-control-flow (my call).
- Roles: `read:companies` is already in `ViewerPermissions`, so every user can
  read effective features. New `read:features` / `manage:features` constants
  are Admin-only (added to `AllPermissions`); registry/recipe management is
  not a Viewer activity (my call).
- Recipes reference features **by Id** (FK), never by free-text code — a typo
  is a `400` with the unknown ids listed, never a silent no-op (my call).
- Delete semantics: feature or recipe still referenced → `409 Conflict`;
  otherwise soft-delete via `IsDeleted`, following the existing `ExpenseType`
  convention. Deleting a company cascades its `CompanyRecipe` rows (my call).
- PUT replace semantics: empty array is a valid, explicit "strip everything";
  unknown company/recipe/feature ids → `400` listing them. No confirmation
  guard — management is endpoint-only, called deliberately by an admin (my call).
- Seeded codes are exactly the lowercase route names: `sales`, `stock`,
  `expenses`. A new page (refunds, livestock) starts with `POST /features`,
  then joins recipes — no backend code change needed to register it.

## Backend gating

```
request ──▶ auth policy (existing) ──▶ RequireFeature("expenses") ──▶ handler
                                            │
                              companyId from route ──▶ effective features
                              missing feature ──▶ 404 (my call: don't leak existence)
```

- Mechanism: endpoint filter (or MediatR behaviour) resolving the company's
  effective features; exact shape left to builder.
- Gated today: company expense-mappings endpoints (`expenses`). Future
  feature-scoped endpoints (livestock, refunds) get the same gate as they land.
- **`/api/expense-types` stays ungated** (my call): it is global reference
  data in our own database — no company scope, no ERPNext call, no failure
  risk. The gate has one mode only: company-scoped routes. The worker decides
  whether to fetch it (below).
- No background jobs calling ERPNext exist in the backend today; if any are
  added, they must apply the same check.

## Frontend gating

```
login / company switch
      │
      ▼
GET /companies (now includes features[] per company) ──▶ FeatureStore
      │
      ├──▶ NavStore: items keyed by feature code, filtered by store
      ├──▶ router: meta.feature on gated routes; guard extends authGuard
      │         off ──▶ redirect "/" (Overview)
      └──▶ cacheSyncWorker: derives company→features from the /companies
                response it already syncs (no protocol change);
                expense-mappings fetched only for companies with expenses;
                expense-types fetched only if ANY synced company has expenses
```

- Feature↔page mapping: route `meta.feature` and nav item `code` both equal
  `Feature.Code`. Nav labels/icons stay frontend-side, keyed by code (my call).
- **Overview is ungated and unchanged** (user): it is the landing route and
  the redirect target, and its ERPNext calls go through the `awesome_dashboard`
  app which is installed on every site — always available, nothing to gate.
  Auth pages (`/login`, `/reset-password`) stay public as today.
- Features are fetched on login and on company switch; company switch already
  re-keys data in `AuthStore`.

## Rollout / seeding

```
migration: create tables
seed:      features for pages that exist today (sales, stock, expenses)
           + one "Default" recipe containing all of them
           + assign Default to every existing company
result:    behaviour unchanged at launch; carve companies up afterwards
new co's:  recipes come in the POST /companies request (no auto-default)
```

- Refunds/Livestock features are seeded when those pages land, not invented now.
- Empty vestigial dirs `backend/Domain/Pages`, `backend/Domain/UserCompanyPages`
  (never committed, no code) are superseded by this design; remove them.

## Open questions — resolved

**O1 — Overview stays as-is (user).** Its ERPNext calls go through the
`awesome_dashboard` Frappe app, which is installed on every site — those
endpoints are always available, so no feature-awareness is needed there.
The missing-app risk applies to *feature-specific* apps (e.g. livestock),
which is what the gates protect.

**O2 — Recipes are an input to company creation (user).** `POST /companies`
takes recipe ids in the request and assigns them in the same command. No
auto-Default, no empty-by-default onboarding. Unknown recipe ids → `400`
(my call); the list is accepted as given, including empty (my call).

## Decisions

| Decision | Whose |
|---|---|
| Feature = domain entity; company↔recipe many-to-many; union semantics | user |
| Recipes live (edits propagate); composition instead of overrides | user |
| UI gating **and** API gating (ERPNext apps not installed everywhere) | user |
| Management via endpoints, no admin UI for now | user |
| Overview unchanged — awesome_dashboard endpoints always available | user |
| Recipes assigned at creation, as input to POST /companies | user |
| Overview ungated; auth pages public | my call |
| Nav metadata frontend-side, keyed by feature code | my call |
| 404 for gated-off endpoints; 409 on in-use feature delete | my call |
| PUT replaces recipe/assignment sets (explicit save) | my call |
| Seed "Default" recipe → existing companies keep all pages at launch | my call |
| Effective features ride on `GET /companies` (one request, all companies) | my call |
| `/api/expense-types` ungated (own-DB reference data); worker fetch conditional | my call |
| Recipes reference features by Id; unknown ids → 400; empty PUT array valid | my call |
| 409 on in-use feature/recipe delete; soft-delete (`IsDeleted`) otherwise | my call |
| Effective-features read = `read:companies` (Viewers have it); management Admin-only | my call |

## Left to the builder

- Exact gating mechanism (endpoint filter vs MediatR behaviour) and its registration.
- EF Core configuration/migration details following existing conventions.
- FeatureStore shape (new store vs extend AuthStore) and fetch timing details.
- Test layout following repo conventions (`dotnet test`, frontend build/lint).

## Verification expectations

- Backend: union semantics, live-recipe propagation, gate returns 404 when
  feature off, management endpoints honour permissions.
- Frontend: nav filtered, direct URL to gated route redirects, sync worker
  skips feature-scoped fetches when feature off.
- Rollout: existing companies see exactly the pages they see today.