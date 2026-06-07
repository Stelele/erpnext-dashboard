import type { Period } from "@/utils/PeriodUtilities";
import { getPreviousPeriod } from "@/utils/PeriodUtilities";
import { getBarChartConfig, getBarChartTitle } from "@/utils/ChartConfigUtilities";
import type { ErpNextService, AccountMappings } from "@/services/ErpNextService";
import type { Payment, CompanyExpenseMapping } from "@/types/Expenses";
import type { StockDetail, DailyStockValue, StockValueSummary } from "@/types/StockDetail";
import type { GroupSummary } from "@/types/MonthSales";
import moment from "moment";

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
  const companyId = authStore.user?.companies?.find((c) => c.name === authStore.company)?.id ?? "";
  const apiBaseUrl = import.meta.env.VITE_API_URL;
  const headers = { Authorization: `Bearer ${authStore.accessToken}` };

  const [expenseMappings, companySettings] = await Promise.all([
    fetch(`${apiBaseUrl}/api/companies/${companyId}/expense-mappings`, { headers }).then((r) => r.ok ? r.json() : []),
    fetch(`${apiBaseUrl}/api/companies/${companyId}/settings`, { headers }).then((r) => r.ok ? r.json() : null),
  ]);

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
