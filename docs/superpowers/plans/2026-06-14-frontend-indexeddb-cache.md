# Frontend IndexedDB Cache Layer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate backend API latency by caching all backend API responses in IndexedDB (Dexie.js), with a web worker for background refresh.

**Architecture:** Replace `idb-keyval` with Dexie. Create a `CachedApiClient` wrapper around the existing `ApiSingleton` that intercepts GET/POST/PUT/DELETE calls, serving from IndexedDB on cache hits. A dedicated web worker polls all backend GET endpoints every 5 minutes and silently updates the Dexie store. On first launch, a bootstrap step populates all tables with a loading indicator.

**Tech Stack:** Dexie.js 4.x, Web Workers (module type, Vite-native), existing openapi-fetch client.

---

### Task 1: Install Dexie, remove idb-keyval

**Files:**
- Modify: `frontend/package.json`

- [ ] **Step 1: Install dexie and remove idb-keyval**

Run:
```bash
npm uninstall idb-keyval && npm install dexie
```
Expected: `idb-keyval` removed from `package.json`, `dexie` added under dependencies.

- [ ] **Step 2: Verify package.json**

Check that `package.json` no longer contains `idb-keyval` and now includes `"dexie": "^4.x"` (or similar 4.x version).

---

### Task 2: Create Dexie database schema

**Files:**
- Create: `frontend/src/services/db/index.ts`

- [ ] **Step 1: Create the Dexie database class**

```typescript
import Dexie, { type Table } from "dexie";
import type { components } from "@/services/api/schema";

export const CURRENT_SCHEMA_VERSION = 1;

export interface CacheMetaEntry {
  key: string;
  dbVersion: number;
  lastFullSync: string;
}

export interface LogoEntry {
  siteId: string;
  url: string;
}

type UserResponse = components["schemas"]["UserResponse"];
type CompanyResponse = components["schemas"]["CompanyResponse"];
type SiteResponse = components["schemas"]["SiteResponse"];
type ExpenseTypeResponse = components["schemas"]["ExpenseTypeResponse"];
type CompanyExpenseMappingResponse = components["schemas"]["CompanyExpenseMappingResponse"];
type CompanySettingsResponse = components["schemas"]["CompanySettingsResponse"];

export class DashboardCacheDB extends Dexie {
  users!: Table<UserResponse, string>;
  companies!: Table<CompanyResponse, string>;
  sites!: Table<SiteResponse, string>;
  expenseTypes!: Table<ExpenseTypeResponse, string>;
  expenseMappings!: Table<CompanyExpenseMappingResponse, string>;
  companySettings!: Table<CompanySettingsResponse, string>;
  chartColors!: Table<{ primaryColor: string; chartColors: string[] }, string>;
  logos!: Table<LogoEntry, string>;
  meta!: Table<CacheMetaEntry, string>;

  constructor() {
    super("DashboardCache");
    this.version(CURRENT_SCHEMA_VERSION).stores({
      users: "id",
      companies: "id",
      sites: "id",
      expenseTypes: "id",
      expenseMappings: "id",
      companySettings: "id",
      chartColors: "primaryColor",
      logos: "siteId",
      meta: "key",
    });
  }
}

let dbInstance: DashboardCacheDB | null = null;

export function getCacheDB(): DashboardCacheDB {
  if (!dbInstance) {
    dbInstance = new DashboardCacheDB();
  }
  return dbInstance;
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit src/services/db/index.ts
```
Expected: No type errors. (May fail due to missing imports in isolation — proceed to next build check.)

---

### Task 3: Create CachedApiClient wrapper

**Files:**
- Create: `frontend/src/services/cache/CachedApiClient.ts`

- [ ] **Step 1: Create CachedApiClient**

```typescript
import { ApiSingleton, type Client } from "@/services/api";
import type { paths } from "@/services/api/schema";
import { getCacheDB, CURRENT_SCHEMA_VERSION, type LogoEntry } from "@/services/db";
import type { components } from "@/services/api/schema";

type UserResponse = components["schemas"]["UserResponse"];
type CompanyResponse = components["schemas"]["CompanyResponse"];
type SiteResponse = components["schemas"]["SiteResponse"];
type ExpenseTypeResponse = components["schemas"]["ExpenseTypeResponse"];
type CompanyExpenseMappingResponse = components["schemas"]["CompanyExpenseMappingResponse"];
type CompanySettingsResponse = components["schemas"]["CompanySettingsResponse"];

export class CachedApiClient {
  private static instance: CachedApiClient | null = null;
  private api!: Client;
  private initialized = false;

  public static getInstance(): CachedApiClient {
    if (!this.instance) {
      this.instance = new CachedApiClient();
    }
    return this.instance;
  }

  async init(): Promise<void> {
    if (this.initialized) return;
    this.api = await ApiSingleton.getInstance();
    this.initialized = true;
  }

  async bootstrap(onProgress?: (current: number, total: number) => void): Promise<void> {
    await this.init();
    const db = getCacheDB();
    const meta = await db.meta.get("singleton");

    if (meta?.dbVersion === CURRENT_SCHEMA_VERSION) {
      return;
    }

    const tasks: (() => Promise<void>)[] = [
      async () => {
        const { data } = await this.api.GET("/users", {});
        if (data) {
          const users = Array.isArray(data) ? data : [data];
          await db.users.bulkPut(users as UserResponse[]);
        }
      },
      async () => {
        const { data } = await this.api.GET("/companies", {});
        if (data) {
          const companies = Array.isArray(data) ? data : [data];
          await db.companies.bulkPut(companies as CompanyResponse[]);
        }
      },
      async () => {
        const { data } = await this.api.GET("/sites", {});
        if (data) {
          const sites = Array.isArray(data) ? data : [data];
          await db.sites.bulkPut(sites as SiteResponse[]);
        }
      },
      async () => {
        const { data } = await this.api.GET("/api/expense-types", {});
        if (data) {
          const types = Array.isArray(data) ? data : [data];
          await db.expenseTypes.bulkPut(types as ExpenseTypeResponse[]);
        }
      },
      async () => {
        const { data } = await this.api.GET("/api/theme/chart-colors", {});
        if (data) {
          const colors = Array.isArray(data) ? data : [data];
          await db.chartColors.bulkPut(colors as { primaryColor: string; chartColors: string[] }[]);
        }
      },
      async () => {
        const companies = await db.companies.toArray();
        for (const company of companies) {
          try {
            const { data } = await this.api.GET("/api/companies/{companyId}/expense-mappings", {
              params: { path: { companyId: company.id } },
            });
            if (data) {
              const mappings = Array.isArray(data) ? data : [data];
              await db.expenseMappings.bulkPut(mappings as CompanyExpenseMappingResponse[]);
            }
          } catch {
            // skip
          }
          try {
            const { data } = await this.api.GET("/api/companies/{companyId}/settings", {
              params: { path: { companyId: company.id } },
            });
            if (data) {
              await db.companySettings.put(data as CompanySettingsResponse);
            }
          } catch {
            // skip
          }
        }
      },
    ];

    let completed = 0;
    for (const task of tasks) {
      await task();
      completed++;
      onProgress?.(completed, tasks.length);
    }

    await db.meta.put({
      key: "singleton",
      dbVersion: CURRENT_SCHEMA_VERSION,
      lastFullSync: new Date().toISOString(),
    });
  }

  async getUser(userId: string): Promise<UserResponse | undefined> {
    const db = getCacheDB();
    const cached = await db.users.get(userId);
    if (cached) return cached;

    const { data, error } = await this.api.GET("/users/{id}", {
      params: { path: { id: userId } },
    });
    if (!error && data) {
      await db.users.put(data as UserResponse);
    }
    return data;
  }

  async getUserCompanies(): Promise<CompanyResponse[]> {
    const db = getCacheDB();
    const cached = await db.companies.toArray();
    if (cached.length > 0) return cached;

    const { data } = await this.api.GET("/companies", {});
    if (data) {
      const companies = Array.isArray(data) ? data : [data];
      await db.companies.bulkPut(companies as CompanyResponse[]);
      return companies as CompanyResponse[];
    }
    return [];
  }

  async getSite(siteId: string): Promise<SiteResponse | undefined> {
    const db = getCacheDB();
    const cached = await db.sites.get(siteId);
    if (cached) return cached;

    const { data, error } = await this.api.GET("/sites/{id}", {
      params: { path: { id: siteId } },
    });
    if (!error && data) {
      await db.sites.put(data as SiteResponse);
    }
    return data;
  }

  async getSiteLogo(siteId: string, companyName: string): Promise<string | undefined> {
    const db = getCacheDB();
    const cacheKey = `${siteId}:${companyName}`;
    const cached = await db.logos.get(cacheKey);
    if (cached) return cached.url;

    try {
      const { data } = await this.api.GET("/sites/{siteId}/logo", {
        params: { path: { siteId }, query: { company: companyName } },
      });
      const url = data?.url || "/logo.png";
      await db.logos.put({ siteId: cacheKey, url });
      return url;
    } catch {
      return "/logo.png";
    }
  }

  async getCompanyExpenseMappings(companyId: string): Promise<CompanyExpenseMappingResponse[]> {
    const db = getCacheDB();
    const all = await db.expenseMappings
      .where("companyId")
      .equals(companyId)
      .toArray();
    if (all.length > 0) return all;

    const { data, error } = await this.api.GET("/api/companies/{companyId}/expense-mappings", {
      params: { path: { companyId } },
    });
    if (!error && data) {
      const mappings = Array.isArray(data) ? data : [data];
      await db.expenseMappings.bulkPut(mappings as CompanyExpenseMappingResponse[]);
      return mappings as CompanyExpenseMappingResponse[];
    }
    return [];
  }

  async getCompanySettings(companyId: string): Promise<CompanySettingsResponse | undefined> {
    const db = getCacheDB();
    const cached = await db.companySettings.get(companyId);
    if (cached) return cached;

    const { data, error } = await this.api.GET("/api/companies/{companyId}/settings", {
      params: { path: { companyId } },
    });
    if (!error && data) {
      await db.companySettings.put(data as CompanySettingsResponse);
    }
    return data;
  }

  async getChartColors(primaryColor: string): Promise<string[] | undefined> {
    const db = getCacheDB();
    const cached = await db.chartColors.get(primaryColor);
    if (cached) return cached.chartColors;

    const { data, error } = await this.api.GET("/api/theme/chart-colors", {
      params: { query: { primaryColor: primaryColor as never } },
    });
    if (!error && data?.chartColors?.length) {
      await db.chartColors.put({
        primaryColor,
        chartColors: data.chartColors as string[],
      });
      return data.chartColors as string[];
    }
    return undefined;
  }
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors in the new file.

---

### Task 4: Create web worker for background refresh

**Files:**
- Create: `frontend/src/services/cache/cacheSyncWorker.ts`

- [ ] **Step 1: Create the worker file**

```typescript
import createClient from "openapi-fetch";
import type { paths } from "@/services/api/schema";

let accessToken = "";
let baseUrl = "";
let intervalId: ReturnType<typeof setInterval> | null = null;

function createApi() {
  return createClient<paths>({
    baseUrl,
    headers: { Authorization: `Bearer ${accessToken}` },
  });
}

async function syncAll(): Promise<void> {
  const api = createApi();
  const endpoints = [
    { name: "users", fn: () => api.GET("/users", {}) },
    { name: "companies", fn: () => api.GET("/companies", {}) },
    { name: "sites", fn: () => api.GET("/sites", {}) },
    { name: "expenseTypes", fn: () => api.GET("/api/expense-types", {}) },
    { name: "chartColors", fn: () => api.GET("/api/theme/chart-colors", {}) },
  ];

  let completed = 0;
  const total = endpoints.length;

  for (const ep of endpoints) {
    try {
      await ep.fn();
    } catch {
      // Silently skip failures — next cycle will retry
    }
    completed++;
    self.postMessage({ type: "SYNC_PROGRESS", current: completed, total });
  }

  // Also sync per-company data
  try {
    const { data: companies } = await api.GET("/companies", {});
    if (companies && Array.isArray(companies)) {
      for (const company of companies) {
        try {
          await api.GET("/api/companies/{companyId}/expense-mappings", {
            params: { path: { companyId: company.id } },
          });
        } catch { /* skip */ }
        try {
          await api.GET("/api/companies/{companyId}/settings", {
            params: { path: { companyId: company.id } },
          });
        } catch { /* skip */ }
      }
    }
  } catch { /* skip */ }

  self.postMessage({ type: "SYNC_COMPLETE" });
}

self.onmessage = (event: MessageEvent) => {
  const { type, token, url } = event.data;

  switch (type) {
    case "SET_CONFIG":
      accessToken = token;
      baseUrl = url;
      break;

    case "START_SYNC":
      if (intervalId) clearInterval(intervalId);
      syncAll();
      intervalId = setInterval(
        syncAll,
        5 * 60 * 1000 + Math.random() * 30000, // 5 min + up to 30s jitter
      );
      break;

    case "STOP_SYNC":
      if (intervalId) {
        clearInterval(intervalId);
        intervalId = null;
      }
      break;
  }
};
```

- [ ] **Step 2: Verify Vite recognizes the worker entry point**

Run:
```bash
npx vue-tsc --noEmit src/services/cache/cacheSyncWorker.ts
```
Expected: No syntax errors. Some type errors from `self`/`postMessage` may appear — these are expected in worker context and can be ignored; Vite handles worker files correctly at build time.

---

### Task 5: Create useCacheSync composable

**Files:**
- Create: `frontend/src/composables/useCacheSync.ts`

- [ ] **Step 1: Create the composable**

```typescript
import { ref, onUnmounted } from "vue";
import { useAuthStore } from "@/stores/AuthStore";

const syncProgress = ref(0);
const syncTotal = ref(0);
const isSyncing = ref(false);
const lastSync = ref<string | null>(null);

let worker: Worker | null = null;

export function useCacheSync() {
  function startSync() {
    if (worker) return;

    worker = new Worker(
      new URL("@/services/cache/cacheSyncWorker.ts", import.meta.url),
      { type: "module" },
    );

    worker.onmessage = (event: MessageEvent) => {
      const { type, current, total } = event.data;
      switch (type) {
        case "SYNC_PROGRESS":
          syncProgress.value = current;
          syncTotal.value = total;
          isSyncing.value = true;
          break;
        case "SYNC_COMPLETE":
          isSyncing.value = false;
          lastSync.value = new Date().toISOString();
          break;
      }
    };

    const authStore = useAuthStore();
    worker.postMessage({
      type: "SET_CONFIG",
      token: authStore.accessToken,
      url: import.meta.env.VITE_API_URL,
    });
    worker.postMessage({ type: "START_SYNC" });
  }

  function stopSync() {
    worker?.postMessage({ type: "STOP_SYNC" });
    worker?.terminate();
    worker = null;
  }

  function updateToken(token: string) {
    worker?.postMessage({ type: "SET_CONFIG", token, url: import.meta.env.VITE_API_URL });
  }

  onUnmounted(() => {
    stopSync();
  });

  return {
    syncProgress,
    syncTotal,
    isSyncing,
    lastSync,
    startSync,
    stopSync,
    updateToken,
  };
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors.

---

### Task 6: Migrate useChartColors from idb-keyval to Dexie

**Files:**
- Modify: `frontend/src/composables/useChartColors.ts`

- [ ] **Step 1: Rewrite useChartColors to use CachedApiClient**

Replace the entire file content with:

```typescript
import { setChartColors } from '@/utils/ChartJsColors'
import { CachedApiClient } from '@/services/cache/CachedApiClient'
import type { PrimaryColor } from '@/services/api/schema'

export function useChartColors() {
  async function loadChartColors(primaryColor: PrimaryColor | null | undefined): Promise<void> {
    if (!primaryColor) {
      setChartColors([])
      return
    }

    try {
      const client = CachedApiClient.getInstance()
      const colors = await client.getChartColors(primaryColor)
      if (colors && colors.length > 0) {
        setChartColors(colors)
      }
    } catch {
      setChartColors([])
    }
  }

  return { loadChartColors }
}
```

- [ ] **Step 2: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors. Import of `idb-keyval` is gone.

---

### Task 7: Update DataStore to use CachedApiClient

**Files:**
- Modify: `frontend/src/stores/DataStore.ts`

- [ ] **Step 1: Replace ApiSingleton import and remove in-memory caches**

Replace the import line:
```typescript
import { ApiSingleton } from "@/services/api";
```
with:
```typescript
import { CachedApiClient } from "@/services/cache/CachedApiClient";
```

- [ ] **Step 2: Replace in-memory cache variables with no-ops (they're unused now)**

Remove the lines:
```typescript
const settingsCache = new Map<string, CompanySettings | null>();
const mappingsCache = new Map<string, CompanyExpenseMapping[]>();
```

- [ ] **Step 3: Update getCompanyExpenseMappings to use CachedApiClient**

Replace the function:
```typescript
  async function getCompanyExpenseMappings(companyId: string): Promise<CompanyExpenseMapping[]> {
    if (mappingsCache.has(companyId)) {
      return mappingsCache.get(companyId)!;
    }
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.GET("/api/companies/{companyId}/expense-mappings", {
      params: { path: { companyId } },
    });
    const result = error ? [] : (data ?? []);
    mappingsCache.set(companyId, result);
    return result;
  }
```

With:
```typescript
  async function getCompanyExpenseMappings(companyId: string): Promise<CompanyExpenseMapping[]> {
    const client = CachedApiClient.getInstance();
    return client.getCompanyExpenseMappings(companyId);
  }
```

- [ ] **Step 4: Update getCompanySettings to use CachedApiClient**

Replace the function:
```typescript
  async function getCompanySettings(companyId: string): Promise<CompanySettings | null> {
    if (settingsCache.has(companyId)) {
      return settingsCache.get(companyId)!;
    }
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.GET("/api/companies/{companyId}/settings", {
      params: { path: { companyId } },
    });
    const result = error ? null : (data ?? null);
    settingsCache.set(companyId, result);
    return result;
  }
```

With:
```typescript
  async function getCompanySettings(companyId: string): Promise<CompanySettings | null> {
    const client = CachedApiClient.getInstance();
    const result = await client.getCompanySettings(companyId);
    return result ?? null;
  }
```

- [ ] **Step 5: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors.

---

### Task 8: Update AuthStore to use CachedApiClient

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts`

- [ ] **Step 1: Replace ApiSingleton import**

Replace:
```typescript
import { ApiSingleton } from "@/services/api";
```
with:
```typescript
import { CachedApiClient } from "@/services/cache/CachedApiClient";
```

- [ ] **Step 2: Update loadSiteData**

Replace the function:
```typescript
  async function loadSiteData(siteId: string) {
    const api = await ApiSingleton.getInstance();
    const { data: site } = await api.GET("/sites/{id}", {
      params: { path: { id: siteId } },
    });
    if (site) {
      siteUrl.value = site.url;
      siteToken.value = site.apiToken;
    }
  }
```

With:
```typescript
  async function loadSiteData(siteId: string) {
    const client = CachedApiClient.getInstance();
    const site = await client.getSite(siteId);
    if (site) {
      siteUrl.value = site.url;
      siteToken.value = site.apiToken;
    }
  }
```

- [ ] **Step 3: Update fetchLogoUrl**

Replace the function:
```typescript
  async function fetchLogoUrl(siteId: string, companyName: string): Promise<string> {
    const cacheKey = `${siteId}:${companyName}`;
    if (logoUrls.value[cacheKey]) return logoUrls.value[cacheKey];

    try {
      const api = await ApiSingleton.getInstance();
      const { data } = await api.GET("/sites/{siteId}/logo", {
        params: { path: { siteId }, query: { company: companyName } },
      });
      const url = data?.url || "/logo.png";
      logoUrls.value[cacheKey] = url;
      return url;
    } catch {
      return "/logo.png";
    }
  }
```

With:
```typescript
  async function fetchLogoUrl(siteId: string, companyName: string): Promise<string> {
    const cacheKey = `${siteId}:${companyName}`;
    if (logoUrls.value[cacheKey]) return logoUrls.value[cacheKey];

    const client = CachedApiClient.getInstance();
    const url = await client.getSiteLogo(siteId, companyName);
    logoUrls.value[cacheKey] = url;
    return url;
  }
```

- [ ] **Step 4: Update update() to use CachedApiClient for user and companies calls**

In the `update()` function, replace:
```typescript
      const api = await ApiSingleton.getInstance();
      const { data } = await api.GET("/users/{id}", {
        params: { path: { id: userId.value } },
      });
      user.value = data;

      // Fetch company details to get site IDs
      if (data?.companies?.length) {
        const { data: allCompanies } = await api.GET("/companies", {
          params: { query: { companyIds: data.companies } },
        });
        companies.value = allCompanies ?? [];
```

With:
```typescript
      const client = CachedApiClient.getInstance();
      const data = await client.getUser(userId.value);
      user.value = data;

      // Fetch company details to get site IDs
      if (data?.companies?.length) {
        const allCompanies = await client.getUserCompanies();
        companies.value = allCompanies.filter(
          (c) => data.companies?.includes(c.id)
        );
```

- [ ] **Step 5: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors.

---

### Task 9: Update App.vue bootstrap flow

**Files:**
- Modify: `frontend/src/App.vue`

- [ ] **Step 1: Add bootstrap call before auth and data fetch**

Replace the script section:

```vue
<script setup lang="ts">
import { onBeforeMount, computed, ref } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import { useCompanyTheme } from "./composables/useCompanyTheme";
import { useCacheSync } from "./composables/useCacheSync";
import { CachedApiClient } from "./services/cache/CachedApiClient";
import { useHead } from "@unhead/vue";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});

const authStore = useAuthStore();
const { startSync } = useCacheSync();
const bootstrapProgress = ref(0);
const bootstrapTotal = ref(0);
const bootstrapping = ref(false);

useHead(
    computed(() => ({
        title: authStore.company
            ? `${authStore.company} Dashboard`
            : "njeremoto-dashboard",
        link: [
            { rel: "icon", href: authStore.logo, type: "image/x-icon" },
            { rel: "shortcut icon", href: authStore.logo, type: "image/x-icon" },
        ],
    })),
);

const { loadAndApply } = useCompanyTheme();

onBeforeMount(async () => {
    await authStore.update();

    const client = CachedApiClient.getInstance();
    await client.init();
    bootstrapping.value = true;
    await client.bootstrap((current, total) => {
        bootstrapProgress.value = current;
        bootstrapTotal.value = total;
    });
    bootstrapping.value = false;

    startSync();
    update();

    const currentCompany = authStore.companies.find(
        (c) => c.name === authStore.company
    );
    if (currentCompany) {
        await loadAndApply(currentCompany.id);
    }
});
</script>
```

The template stays the same — the existing `LoadingBar.vue` is used within the layout already and is driven by `dataStore.loading`. If you want bootstrap-specific loading UI, add it to the layout, but it's not required for this plan.

- [ ] **Step 2: Verify it compiles**

Run:
```bash
npx vue-tsc --noEmit
```
Expected: No type errors.

---

### Task 10: Build and verify

**Files:** None (verification only)

- [ ] **Step 1: Full type check**

Run:
```bash
npm run build:test
```
Expected: Clean build with no errors.

- [ ] **Step 2: Verify imports are clean**

Run:
```bash
grep -r "idb-keyval" frontend/src/ || echo "No idb-keyval imports remain"
```
Expected: "No idb-keyval imports remain"

- [ ] **Step 3: Verify Dexie is in package.json**

Run:
```bash
grep '"dexie"' frontend/package.json
```
Expected: Shows the dexie dependency line.

- [ ] **Step 4: Verify all new files exist**

Run:
```bash
ls -la frontend/src/services/db/index.ts frontend/src/services/cache/CachedApiClient.ts frontend/src/services/cache/cacheSyncWorker.ts frontend/src/composables/useCacheSync.ts
```
Expected: All four files exist.

---

### Task 11: Manual smoke test checklist

- [ ] Navigate to the app fresh (clear IndexedDB first via DevTools > Application > IndexedDB > delete DashboardCache)
- [ ] Observe bootstrap progress indicator — should show count incrementing
- [ ] After bootstrap completes, dashboard data loads from Dexie (visible in Network tab: no calls to backend API for cached resources)
- [ ] Verify chart colors render correctly (use `useChartColors` now uses Dexie)
- [ ] Wait 5 minutes — worker should silently refresh (check IndexedDB timestamps in DevTools)
- [ ] Refresh page — bootstrap should skip (meta.dbVersion matches CURRENT_SCHEMA_VERSION)
- [ ] Switch companies — settings/logo/mappings should load from cache
