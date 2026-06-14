import createClient from "openapi-fetch";
import type { paths, components } from "@/services/api/schema";
import { getCacheDB, type CachedExpenseMapping } from "@/services/db";

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

  // Fetch reference data (parallel)
  const results = await Promise.allSettled([
    (async () => {
      const { data } = await api.GET("/users", {});
      if (data) await db.users.bulkPut(data as never);
    })(),
    (async () => {
      const { data } = await api.GET("/companies", {});
      if (data) await db.companies.bulkPut(data as never);
    })(),
    (async () => {
      const { data } = await api.GET("/sites", {});
      if (data) await db.sites.bulkPut(data as never);
    })(),
    (async () => {
      const { data } = await api.GET("/api/expense-types", {});
      if (data) await db.expenseTypes.bulkPut(data as never);
    })(),
  ]);

  // Report errors from reference data fetches
  for (const result of results) {
    if (result.status === "rejected") {
      self.postMessage({ type: "SYNC_ERROR", context: "reference-data", error: String(result.reason) });
    }
  }

  // Fetch per-company data
  try {
    const companies = await db.companies.toArray();
    const totalEndpoints = 4 + companies.length;
    self.postMessage({ type: "SYNC_PROGRESS", current: 4, total: totalEndpoints });

    for (const company of companies) {
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
