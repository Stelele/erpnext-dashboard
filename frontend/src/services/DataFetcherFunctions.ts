import type { Period } from "@/utils/PeriodUtilities";
import { getPreviousPeriod } from "@/utils/PeriodUtilities";
import { getBarChartConfig, getBarChartTitle } from "@/utils/ChartConfigUtilities";
import type { ErpNextService, AccountMappings } from "@/services/ErpNextService";
import type { Payment } from "@/types/Expenses";
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

  const [
    dashboardResults,
    accountMappingsData,
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
    erpNextService.getAccountMappings(),
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

  return {
    dashboardResults,
    accountMappings: accountMappingsData,
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
