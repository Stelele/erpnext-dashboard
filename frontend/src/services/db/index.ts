import Dexie, { type Table } from "dexie";
import type { components } from "@/services/api/schema";

export const CURRENT_SCHEMA_VERSION = 3;

export interface CacheMetaEntry {
  key: string;
  dbVersion: number;
  lastFullSync: string;
  userId?: string;
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

export interface CachedExpenseMapping extends CompanyExpenseMappingResponse {
  companyId: string;
}

export class DashboardCacheDB extends Dexie {
  users!: Table<UserResponse, string>;
  companies!: Table<CompanyResponse, string>;
  sites!: Table<SiteResponse, string>;
  expenseTypes!: Table<ExpenseTypeResponse, string>;
  expenseMappings!: Table<CachedExpenseMapping, [string, string]>;
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
      expenseMappings: "[companyId+id]",
      companySettings: "companyId",
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
