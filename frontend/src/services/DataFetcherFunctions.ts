import type { Period } from "@/utils/PeriodUtilities";
import { getPreviousPeriod } from "@/utils/PeriodUtilities";
import { getBarChartConfig, getBarChartTitle } from "@/utils/ChartConfigUtilities";
import type { ErpNextService, AccountMappings } from "@/services/ErpNextService";
import type { Payment, CompanyExpenseMapping } from "@/types/Expenses";
import type { StockDetail, DailyStockValue, StockValueSummary } from "@/types/StockDetail";
import type { GroupSummary } from "@/types/MonthSales";
import moment from "moment";
import { ApiSingleton } from "@/services/api";

interface ExpenseTypeData {
  expense_type: string;
  total: number;
  count: number;
}

interface OrderTypeData {
  description: string;
  total: number;
}

interface DashboardInterval {
  grouping_name: string;
  total: number;
}

export interface DataFetchResult {
  dashboardResults: any[];
  accountMappings: AccountMappings;
  expenseMappings: CompanyExpenseMapping[];
  paymentEntries: Payment[];
  stockDetails: StockDetail[];
  dailyStockValues: DailyStockValue[];
  aggregatedSales: any;
  barChartData: any;
  barChartConfig: { fromDate: string; toDate: string; grouping: "day" | "week" | "month" | "quarter" };
  barChartTitle: string;
  stockValueData: StockValueSummary[];
  salesSummaryData: GroupSummary[];
  orderBreakdownData: OrderTypeData[];
  prevExpensesData: DashboardInterval[];
  expenseBreakdownData: ExpenseTypeData[];
}

export async function fetchAllData(
  period: Period,
  erpNextService: ErpNextService,
): Promise<DataFetchResult> {
  const prevPeriod = getPreviousPeriod(period);
  const barChartConfig = getBarChartConfig(period);
  const barChartTitle = getBarChartTitle(period);

  const stockPeriod = ["Today", "This Week"].includes(period) ? "This Week" : "Last 12 Months";

  const authStore = await import("@/stores/AuthStore").then((m) => m.useAuthStore());
  const companyId = authStore.companies?.find((c) => c.name === authStore.company)?.id;
  const api = await ApiSingleton.getInstance();

  const [expenseMappingsResult, companySettingsResult] = companyId
    ? await Promise.all([
        api.GET("/api/companies/{companyId}/expense-mappings", { params: { path: { companyId } } }),
        api.GET("/api/companies/{companyId}/settings", { params: { path: { companyId } } }),
      ])
    : [{ error: true, data: null }, { error: true, data: null }];

  const expenseMappings = expenseMappingsResult.error ? [] : (expenseMappingsResult.data ?? []).map((m) => ({
    expenseTypeId: m.expenseTypeId,
    expenseTypeName: m.expenseTypeName,
    erpnextAccountName: m.erpnextAccountName,
  }));
  const companySettings = companySettingsResult.error ? null : {
    id: companySettingsResult.data!.id,
    companyId: companySettingsResult.data!.companyId,
    defaultIncomeAccountName: companySettingsResult.data!.defaultIncomeAccountName,
  };

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

  return {
    dashboardResults,
    accountMappings: accountMappingsData,
    expenseMappings: expenseMappings ?? [],
    paymentEntries: paymentEntriesData,
    stockDetails: stockDetailsData,
    dailyStockValues,
    aggregatedSales,
    barChartData,
    barChartConfig,
    barChartTitle,
    stockValueData,
    salesSummaryData,
    orderBreakdownData,
    prevExpensesData,
    expenseBreakdownData,
  };
}
