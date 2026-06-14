import { ApiSingleton, type Client } from "@/services/api";
import type { components } from "@/services/api/schema";
import { getCacheDB, CURRENT_SCHEMA_VERSION, type CachedExpenseMapping } from "@/services/db";

type UserResponse = components["schemas"]["UserResponse"];
type CompanyResponse = components["schemas"]["CompanyResponse"];
type SiteResponse = components["schemas"]["SiteResponse"];
type ExpenseTypeResponse = components["schemas"]["ExpenseTypeResponse"];
type CompanyExpenseMappingResponse = components["schemas"]["CompanyExpenseMappingResponse"];
type CompanySettingsResponse = components["schemas"]["CompanySettingsResponse"];

export class CachedApiClient {
  private constructor() {}
  private static instance: CachedApiClient | null = null;
  private api: Client | null = null;

  public static getInstance(): CachedApiClient {
    if (!this.instance) {
      this.instance = new CachedApiClient();
    }
    return this.instance;
  }

  async init(): Promise<void> {
    if (this.api) return;
    this.api = await ApiSingleton.getInstance();
  }

  private async ensureApi(): Promise<Client> {
    if (!this.api) {
      await this.init();
    }
    return this.api!;
  }

  async bootstrap(userId?: string, onProgress?: (current: number, total: number) => void): Promise<void> {
    const api = await this.ensureApi();
    const db = getCacheDB();
    const meta = await db.meta.get("singleton");

    if (userId && meta?.userId && meta.userId !== userId) {
      await db.delete();
      await db.open();
    }

    const refreshedMeta = await db.meta.get("singleton");
    if (refreshedMeta?.dbVersion === CURRENT_SCHEMA_VERSION && refreshedMeta?.userId === userId) {
      return;
    }

    const tasks: (() => Promise<void>)[] = [
      async () => {
        const { data } = await api.GET("/users", {});
        if (data) {
          await db.users.bulkPut(Array.isArray(data) ? data as UserResponse[] : [data as unknown as UserResponse]);
        }
      },
      async () => {
        const { data } = await api.GET("/companies", {});
        if (data) {
          await db.companies.bulkPut(Array.isArray(data) ? data as CompanyResponse[] : [data as unknown as CompanyResponse]);
        }
      },
      async () => {
        const { data } = await api.GET("/sites", {});
        if (data) {
          await db.sites.bulkPut(Array.isArray(data) ? data as SiteResponse[] : [data as unknown as SiteResponse]);
        }
      },
      async () => {
        const { data } = await api.GET("/api/expense-types", {});
        if (data) {
          await db.expenseTypes.bulkPut(Array.isArray(data) ? data as ExpenseTypeResponse[] : [data as unknown as ExpenseTypeResponse]);
        }
      },
      async () => {
        const companies = await db.companies.toArray();
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
          } catch { /* skip */ }
          try {
            const { data } = await api.GET("/api/companies/{companyId}/settings", {
              params: { path: { companyId: company.id } },
            });
            if (data) {
              await db.companySettings.put(data as CompanySettingsResponse);
            }
          } catch { /* skip */ }
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
      userId: userId || "",
    });
  }

  async getUser(userId: string): Promise<UserResponse | undefined> {
    const db = getCacheDB();
    const cached = await db.users.get(userId);
    if (cached) return cached;

    const api = await this.ensureApi();
    const { data, error } = await api.GET("/users/{id}", {
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

    const api = await this.ensureApi();
    const { data, error } = await api.GET("/companies", {});
    if (!error && data) {
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

    const api = await this.ensureApi();
    const { data, error } = await api.GET("/sites/{id}", {
      params: { path: { id: siteId } },
    });
    if (!error && data) {
      await db.sites.put(data as SiteResponse);
    }
    return data;
  }

  async getSiteLogo(siteId: string, companyName: string): Promise<string> {
    const db = getCacheDB();
    const cacheKey = `${siteId}:${companyName}`;
    const cached = await db.logos.get(cacheKey);
    if (cached) return cached.url;

    try {
      const api = await this.ensureApi();
      const { data } = await api.GET("/sites/{siteId}/logo", {
        params: { path: { siteId }, query: { company: companyName } },
      });
      const url = data?.url || "/logo.png";
      await db.logos.put({ siteId: cacheKey, url });
      return url;
    } catch {
      return "/logo.png";
    }
  }

  async getCompanyExpenseMappings(companyId: string): Promise<CachedExpenseMapping[]> {
    const db = getCacheDB();
    const all = await db.expenseMappings
      .where("companyId")
      .equals(companyId)
      .toArray();
    if (all.length > 0) return all;

    const api = await this.ensureApi();
    const { data, error } = await api.GET("/api/companies/{companyId}/expense-mappings", {
      params: { path: { companyId } },
    });
    if (!error && data) {
      const mappings = (Array.isArray(data) ? data : [data]) as CompanyExpenseMappingResponse[];
      const withCompanyId: CachedExpenseMapping[] = mappings.map((m) => ({
        ...m,
        companyId,
      }));
      await db.expenseMappings.bulkPut(withCompanyId);
      return withCompanyId;
    }
    return [];
  }

  async getCompanySettings(companyId: string): Promise<CompanySettingsResponse | undefined> {
    const db = getCacheDB();
    const cached = await db.companySettings.get(companyId);
    if (cached) return cached;

    const api = await this.ensureApi();
    const { data, error } = await api.GET("/api/companies/{companyId}/settings", {
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

    try {
      const authStore = (await import("@/stores/AuthStore")).useAuthStore();
      const response = await fetch(
        `${import.meta.env.VITE_API_URL}/api/theme/chart-colors?primaryColor=${encodeURIComponent(primaryColor)}`,
        { headers: { Authorization: `Bearer ${authStore.accessToken}` } }
      );
      if (response.ok) {
        const result = await response.json() as { chartColors?: string[] };
        if (result.chartColors?.length) {
          await db.chartColors.put({
            primaryColor,
            chartColors: result.chartColors,
          });
          return result.chartColors;
        }
      }
    } catch {
      // fall through
    }
    return undefined;
  }
}
