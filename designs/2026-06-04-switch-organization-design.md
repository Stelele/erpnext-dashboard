# Switch Organization Feature Design

## Overview

Add a mechanism for users to switch between organizations (companies) they belong to. The switch updates the company name, logo, and re-fetches all dashboard data without a full page reload.

## Requirements

- Sidebar footer shows a "Switch Company" button only when user belongs to more than 2 companies
- Clicking the button opens a modal showing available companies with logos
- Company logos are fetched from each company's ERPNext site API
- On switch: update company context, show loading bar, re-fetch all dashboard data
- If data re-fetch fails, revert to previous company and show error
- If logo fetch fails, show a fallback placeholder

## Architecture

### AuthStore Changes

- Add `selectedCompany` as a `ref<string>` (defaults to first company in `user.companies`)
- Change `company` computed from hardcoded `"Njeremoto Enterprises"` to derive from `selectedCompany`
- `url` and `token` computeds already derive from `company` — no change needed
- Add `switchCompany(companyName: string)` method that:
  1. Saves current company for rollback
  2. Updates `selectedCompany`
  3. Triggers data re-fetch via `dataStore.update()`
  4. On failure, reverts to previous company

### New Component: CompanySwitcherModal.vue

- Modal dialog showing list of companies from `authStore.user.companies`
- Each item shows company name and logo (fetched from ERPNext)
- Current company is highlighted
- Clicking a company calls `authStore.switchCompany()`
- Uses Nuxt UI components (`UModal`, `UButton`, etc.) consistent with existing codebase

### Sidebar Changes

- Add "Switch Company" button to `SideBar.vue` footer
- Visibility: `v-if="authStore.user?.companies && authStore.user.companies.length > 2"`
- Button placed above the "Last Refresh" text

### Logo Fetching

- Add `getCompanyLogo(companyName: string, siteUrl: string, siteToken: string)` static helper to `ErpNextService`
- Uses a direct axios call (not the instance from constructor) with the target company's site URL and token
- Fetches from ERPNext `/api/resource/Company/{companyName}` and reads `company_logo` field
- Returns logo URL string or undefined
- Component handles loading/error states per-company
- Logos are fetched in parallel when modal opens using `Promise.allSettled`

### Persistence

- Selected company is persisted to `localStorage` under key `selectedCompany`
- On app init (`AuthStore.update()`), if `localStorage` has a valid company name that exists in `user.companies`, use it; otherwise default to first company
- This ensures the user's selection survives page refreshes and browser restarts

## Data Flow

```
User clicks "Switch Company"
  → Modal opens
  → For each company: fetch logo from its ERPNext site
User selects a company
  → authStore.switchCompany(companyName) called
  → selectedCompany updated → company/url/token computeds react
  → dataStore.loading = true → LoadingBar shows
  → dataStore.update() re-fetches all dashboard endpoints
  → All charts/tables update reactively
  → dataStore.loading = false → LoadingBar hides
  → Modal closes
```

## Error Handling

- Logo fetch failure: show company initial in a colored circle as fallback
- Data re-fetch failure: revert `selectedCompany` to previous value, show error notification
- Network timeout during switch: same as re-fetch failure
- User has 0-2 companies: switcher button is not rendered

## Files Modified

- `frontend/src/stores/AuthStore.ts` — add selectedCompany, switchCompany
- `frontend/src/components/SideBar.vue` — add switch button
- `frontend/src/services/ErpNextService.ts` — add getCompanyLogo
- `frontend/src/components/CompanySwitcherModal.vue` — new component

## Dependencies

- No new npm packages required
- Uses existing Nuxt UI modal components
- Uses existing Pinia store patterns
- Uses existing axios-based ERPNext service
