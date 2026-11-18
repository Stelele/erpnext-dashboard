import createClient from "openapi-fetch";
import type { paths, components } from "@/services/api/schema";
import { getCacheDB, type CachedExpenseMapping } from "@/services/db";
import { FEATURES } from "@/constants/features";

type CompanyExpenseMappingResponse = components["schemas"]["CompanyExpenseMappingResponse"];
type CompanySettingsResponse = components["schemas"]["CompanySettingsResponse"];

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
  const db = getCacheDB();

  // Fetch companies FIRST to derive feature gating for expense-scoped sync
  let companiesFresh = false;
  try {
    const { data } = await api.GET("/companies", {});
    if (data) {
      await db.companies.clear();
      await db.companies.bulkPut(data as never);
      companiesFresh = true;
    }
  } catch (err) {
    self.postMessage({ type: "SYNC_ERROR", context: "reference-data", error: String(err) });
  }

  const syncedCompanies = await db.companies.toArray();
  const syncExpenses = companiesFresh && syncedCompanies.some((c) => c.features?.includes(FEATURES.expenses));

  // Fetch remaining reference data (parallel); expense-types only if any company has the feature
  const referenceTasks: Promise<void>[] = [
      (async () => {
        const { data } = await api.GET("/users", {});
        if (data) {
          await db.users.clear();
          await db.users.bulkPut(data as never);
        }
      })(),
      (async () => {
        const { data } = await api.GET("/sites", {});
        if (data) {
          await db.sites.clear();
          await db.sites.bulkPut(data as never);
        }
      })(),
    ];
  if (syncExpenses) {
    referenceTasks.push(
      (async () => {
        const { data } = await api.GET("/api/expense-types", {});
        if (data) {
          await db.expenseTypes.clear();
          await db.expenseTypes.bulkPut(data as never);
        }
      })(),
    );
  }
  const results = await Promise.allSettled(referenceTasks);

  // Report errors from reference data fetches
  for (const result of results) {
    if (result.status === "rejected") {
      self.postMessage({ type: "SYNC_ERROR", context: "reference-data", error: String(result.reason) });
    }
  }

  // Fetch per-company data
  try {
    const companies = await db.companies.toArray();
    const referenceCount = syncExpenses ? 4 : 3;
    const expenseCompanyCount = companiesFresh ? companies.filter((c) => c.features?.includes(FEATURES.expenses)).length : 0;
    const totalEndpoints = referenceCount + companies.length + expenseCompanyCount;
    self.postMessage({ type: "SYNC_PROGRESS", current: referenceCount, total: totalEndpoints });

    await db.expenseMappings.clear();
    await db.companySettings.clear();

    for (const company of companies) {
      if (companiesFresh && company.features?.includes(FEATURES.expenses)) {
        try {
          const { data } = await api.GET("/api/companies/{companyId}/expense-mappings", {
            params: { path: { companyId: company.id } },
          });
          if (data) {
            const mappings = (Array.isArray(data) ? data : [data]) as CompanyExpenseMappingResponse[];
            const withCompanyId: CachedExpenseMapping[] = mappings.map((m) => ({
              ...m,
              companyId: company.id,
            }));
            await db.expenseMappings.bulkPut(withCompanyId);
          }
        } catch (err) {
          self.postMessage({ type: "SYNC_ERROR", context: "expense-mappings", companyId: company.id, error: String(err) });
        }
      }
      try {
        const { data } = await api.GET("/api/companies/{companyId}/settings", {
          params: { path: { companyId: company.id } },
        });
        if (data) {
          await db.companySettings.put(data as CompanySettingsResponse);
        }
      } catch (err) {
        self.postMessage({ type: "SYNC_ERROR", context: "company-settings", companyId: company.id, error: String(err) });
      }
    }

    self.postMessage({ type: "SYNC_PROGRESS", current: totalEndpoints, total: totalEndpoints });
  } catch (err) {
    self.postMessage({ type: "SYNC_ERROR", context: "per-company-sync", error: String(err) });
  }

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
        5 * 60 * 1000 + Math.random() * 30000,
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
