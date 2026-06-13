# Cold Load Parallelization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate sequential bottlenecks in auth and data fetching to reduce cold-load time from ~50s to ~30s by parallelizing independent API calls.

**Architecture:** Three small, independent changes: (1) Run `loadCurrentLogo()`, `loadAllLogos()`, and `loadSiteData()` in parallel during auth bootstrap, (2) Convert `loadAllLogos()` from serial `for...of` to `Promise.all`, (3) Move the `getAccountMappings` ERPNext call into the existing 11-way `Promise.all` batch. All three are independent and safe because the functions handle their own errors internally and do not share mutable state that would conflict under concurrency.

**Tech Stack:** TypeScript, Vue 3, Pinia

**Verification:** `npm run build:test` (vue-tsc + vite build) — no frontend test framework exists in this project.

---

### Task 1: Parallelize auth bootstrap in `AuthStore.update()` and `switchCompany()`

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts:140-144`
- Modify: `frontend/src/stores/AuthStore.ts:153-155`
- Modify: `frontend/src/stores/AuthStore.ts:172-175`

**Rationale:** `loadCurrentLogo()`, `loadAllLogos()`, and `loadSiteData()` hit independent API endpoints and write to non-overlapping reactive state. Running them in parallel saves ~2.5s on cold load. Same pattern applies to the restore-company and switch-company paths.

- [ ] **Step 1: Parallelize the first company load block (lines 140-144)**

Replace:
```ts
        if (selected) {
          await loadCurrentLogo();
          await loadAllLogos();
          await loadSiteData(selected.siteId);
        }
```

With:
```ts
        if (selected) {
          await Promise.all([
            loadCurrentLogo(),
            loadAllLogos(),
            loadSiteData(selected.siteId),
          ]);
        }
```

- [ ] **Step 2: Parallelize the restore-company block (lines 153-155)**

Replace:
```ts
          await loadCurrentLogo();
          await loadSiteData(restored.siteId);
```

With:
```ts
          await Promise.all([
            loadCurrentLogo(),
            loadSiteData(restored.siteId),
          ]);
```

- [ ] **Step 3: Parallelize the switchCompany block (lines 172-175)**

Replace:
```ts
    if (selected) {
      await loadCurrentLogo();
      await loadSiteData(selected.siteId);
    }
```

With:
```ts
    if (selected) {
      await Promise.all([
        loadCurrentLogo(),
        loadSiteData(selected.siteId),
      ]);
    }
```

- [ ] **Step 4: Verify build**

Run: `npm run build:test`
Workdir: `frontend`
Expected: zero TypeScript errors, successful build.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/stores/AuthStore.ts
git commit -m "perf: parallelize auth bootstrap logo and site data fetches"
```

---

### Task 2: Convert `loadAllLogos()` from serial `for...of` to `Promise.all`

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts:92-98`

**Rationale:** With 2 companies, the serial loop doubles the wait. With N companies it scales linearly. `fetchLogoUrl()` already has an internal cache guard and error handling, so parallel calls to different cache keys are safe.

- [ ] **Step 1: Replace the serial loop**

Replace:
```ts
  async function loadAllLogos() {
    for (const c of companies.value) {
      if (c.siteId) {
        await fetchLogoUrl(c.siteId, c.name);
      }
    }
  }
```

With:
```ts
  async function loadAllLogos() {
    await Promise.all(
      companies.value
        .filter((c) => c.siteId)
        .map((c) => fetchLogoUrl(c.siteId, c.name)),
    );
  }
```

- [ ] **Step 2: Verify build**

Run: `npm run build:test`
Workdir: `frontend`
Expected: zero TypeScript errors, successful build.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/stores/AuthStore.ts
git commit -m "perf: run all company logo fetches in parallel"
```

---

### Task 3: Move `getAccountMappings()` into the parallel ERPNext batch

**Files:**
- Modify: `frontend/src/services/DataFetcherFunctions.ts:77-106`

**Rationale:** `getAccountMappings()` calls `account_names` on ERPNext and only depends on `expenseMappings` and `companySettings`, which are already resolved at line 75 (before the 11-way `Promise.all` starts). Currently it runs sequentially after the 11 calls complete. Moving it into the batch saves one full request round-trip (~400-900ms in production).

- [ ] **Step 1: Add `getAccountMappings` to the `Promise.all` destructuring**

Replace lines 77-106:
```ts
  const [
    dashboardResults,
    paymentEntriesData,
    stockDetailsData,
    dailyStockValues,
    aggregatedSales,
    barChartData,
    stockValueData,
    salesSummaryData,
    orderBreakdownData,
    prevExpensesData,
    expenseBreakdownData,
  ] = await Promise.all([
    erpNextService.getDashboardComplete(period, prevPeriod),
    erpNextService.getDashboardPaymentEntries(period),
    erpNextService.getStockLevels(),
    erpNextService.getDailyStockValueSummary("months", 3),
    erpNextService.getDashboardSalesAggregated(period),
    erpNextService.getDashboardBarChart(barChartConfig.fromDate, barChartConfig.toDate, barChartConfig.grouping),
    erpNextService.getStockValueSummary(stockPeriod),
    erpNextService.getSalesSummary(stockPeriod),
    erpNextService.getOrderBreakdown(period),
    erpNextService.getPrevGroupedExpenses("months", 6),
    erpNextService.getExpenseBreakdown(period),
  ]);

  const accountMappingsData = await erpNextService.getAccountMappings(
    expenseMappings ?? [],
    companySettings?.defaultIncomeAccountName ?? "",
  );
```

With:
```ts
  const [
    dashboardResults,
    paymentEntriesData,
    stockDetailsData,
    dailyStockValues,
    aggregatedSales,
    barChartData,
    stockValueData,
    salesSummaryData,
    orderBreakdownData,
    prevExpensesData,
    expenseBreakdownData,
    accountMappingsData,
  ] = await Promise.all([
    erpNextService.getDashboardComplete(period, prevPeriod),
    erpNextService.getDashboardPaymentEntries(period),
    erpNextService.getStockLevels(),
    erpNextService.getDailyStockValueSummary("months", 3),
    erpNextService.getDashboardSalesAggregated(period),
    erpNextService.getDashboardBarChart(barChartConfig.fromDate, barChartConfig.toDate, barChartConfig.grouping),
    erpNextService.getStockValueSummary(stockPeriod),
    erpNextService.getSalesSummary(stockPeriod),
    erpNextService.getOrderBreakdown(period),
    erpNextService.getPrevGroupedExpenses("months", 6),
    erpNextService.getExpenseBreakdown(period),
    erpNextService.getAccountMappings(
      expenseMappings ?? [],
      companySettings?.defaultIncomeAccountName ?? "",
    ),
  ]);
```

- [ ] **Step 2: Verify build**

Run: `npm run build:test`
Workdir: `frontend`
Expected: zero TypeScript errors, successful build.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/services/DataFetcherFunctions.ts
git commit -m "perf: fetch account mappings in parallel with dashboard data"
```

---

## Self-Review

**Spec coverage:**
- Fix 1 (parallelize auth bootstrap): Tasks 1 — covers `update()`, `switchCompany()`, and restore path
- Fix 2 (parallelize loadAllLogos): Task 2 — converts serial loop to `Promise.all`
- Fix 3 (parallelize account_names): Task 3 — moves into the existing `Promise.all` batch
- Fix 4 (defer loadAllLogos): Intentionally skipped — Fix 2 makes this negligible by running logos in parallel. Company switcher already uses cached URLs.

**Placeholder scan:** No TBDs, TODOs, or unspecified code. All code replacements are exact string matches against the current source.

**Type consistency:** `getAccountMappings()` returns `Promise<AccountMappings>` which is the same type as `accountMappingsData` was previously — the destructuring assignment type flows through correctly.
