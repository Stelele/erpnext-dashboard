import { defineStore } from "pinia";
import { ErpNextService, type AccountMappings } from "../services/ErpNextService";
import { computed, ref } from "vue";
import { getPeriodDateRange, getPreviousPeriod, type Period } from "../utils/PeriodUtilities";
import moment from "moment";
import type { Expense, Payment } from "../types/Expenses";
import type { StockDetail } from "@/types/StockDetail";

export const useDataStore = defineStore("dataStore", () => {
  const loading = ref(true);
  const accountMappings = ref<AccountMappings>({
    incomes: {},
    expenses: {},
  } as AccountMappings);
  const paymentEntries = ref<Payment[]>([]);
  const stockDetails = ref<StockDetail[]>([]);

  const currentPeriod = ref<Period>("This Month");
  const lastRefresh = ref("");
  const dateRange = computed(() => getPeriodDateRange(currentPeriod.value));

  function getBarChartConfig(period: Period) {
    const today = moment();
    const monthStart = today.clone().startOf("month");

    switch (period) {
      case "Today":
      case "Yesterday":
        return {
          fromDate: monthStart.clone().subtract(14, "days").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "day" as const,
        };
      case "This Week":
      case "Last Week":
        return {
          fromDate: monthStart.clone().subtract(5, "weeks").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "week" as const,
        };
      case "This Month":
      case "Last Month":
        return {
          fromDate: monthStart.clone().subtract(3, "months").startOf("month").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "month" as const,
        };
      case "This Quarter":
      case "Last Quarter":
        return {
          fromDate: monthStart.clone().subtract(4, "quarters").startOf("quarter").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "quarter" as const,
        };
      case "This Semester":
      case "Last Semester":
        return {
          fromDate: monthStart.clone().subtract(6, "quarters").startOf("quarter").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "quarter" as const,
        };
      case "This Year":
      case "Last Year":
        return {
          fromDate: monthStart.clone().subtract(3, "years").startOf("years").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "quarter" as const,
        };
      default:
        return {
          fromDate: monthStart.clone().subtract(14, "days").format("YYYY-MM-DD"),
          toDate: today.format("YYYY-MM-DD"),
          grouping: "day" as const,
        };
    }
  }

  function getBarChartTitle(period: Period): string {
    switch (period) {
      case "Today":
      case "Yesterday":
        return "Sales from last 14 days";
      case "This Week":
      case "Last Week":
        return "Sales from last 4 weeks";
      case "This Month":
      case "Last Month":
        return "Sales from last 3 months";
      case "This Quarter":
      case "Last Quarter":
        return "Sales from last 4 quarters";
      case "This Semester":
      case "Last Semester":
        return "Sales from last 6 quarters";
      case "This Year":
      case "Last Year":
        return "Sales from last 3 years";
      default:
        return "Sales from last 14 days";
    }
  }

  async function getData() {
    const erpNextService = new ErpNextService();
    const period = currentPeriod.value;
    const prevPeriod = getPreviousPeriod(period);

    lastRefresh.value = moment().format("DD-MMM-YY HH:mm");

    const barChartConfig = getBarChartConfig(period);
    const barChartTitle = getBarChartTitle(period);

    const [
      dashboardResults,
      accountMappingsData,
      paymentEntriesData,
      stockDetailsData,
      dailyStockValues,
      aggregatedSales,
      barChartData,
    ] = await Promise.all([
      erpNextService.getDashboardComplete(period, prevPeriod),
      erpNextService.getAccountMappings(),
      erpNextService.getDashboardPaymentEntries(period),
      erpNextService.getStockLevels(),
      erpNextService.getDailyStockValueSummary("months", 3),
      erpNextService.getDashboardSalesAggregated(period),
      erpNextService.getDashboardBarChart(barChartConfig.fromDate, barChartConfig.toDate, barChartConfig.grouping),
    ]);

    accountMappings.value = accountMappingsData;
    paymentEntries.value = paymentEntriesData;
    stockDetails.value = stockDetailsData;

    const { useOverViewDataStore } = await import("./OverViewDataStore");
    const { useExpenseDataStore } = await import("./ExpenseDataStore");
    const { useStockDataStore } = await import("./StockDataStore");
    const { useSalesDataStore } = await import("./SalesDataStore");

    useOverViewDataStore().parseDashboardResults(dashboardResults);
    useOverViewDataStore().applyBarChart(
      { ...barChartData, fromDate: barChartConfig.fromDate, toDate: barChartConfig.toDate },
      barChartTitle,
      barChartConfig.grouping
    );
    useExpenseDataStore().parseDashboardResults(dashboardResults);
    useStockDataStore().parseDashboardResults(dashboardResults);
    useStockDataStore().setStockTableData(stockDetailsData);
    useStockDataStore().setDailyStockValues({
      labels: dailyStockValues.map(d => moment(d.posting_date).format("DD MMM")),
      datasets: [{
        label: "Closing Stock",
        data: dailyStockValues.map(d => d.daily_stock_value),
      }],
    });
    useSalesDataStore().applyAggregatedSales(aggregatedSales);
  }

  function addDraftExpense(expense: Expense) {
    const erpNextService = new ErpNextService();
    const incomeAccount = accountMappings.value.incomes["Cash"];
    const expenseAccount = accountMappings.value.expenses[expense.expenseType];
    return erpNextService.addDraftExpenseJournalEntry(
      expense,
      incomeAccount,
      expenseAccount,
    );
  }

  async function bulkAddDraftExpenses(
    expenses: Expense[],
  ): Promise<{ success: boolean; expense: Expense; error?: Error }[]> {
    const results: { success: boolean; expense: Expense; error?: Error }[] = [];
    const batchSize = 20;

    for (let i = 0; i < expenses.length; i += batchSize) {
      const batch = expenses.slice(i, i + batchSize);
      const batchResults = await Promise.allSettled(
        batch.map((expense) => addDraftExpense(expense)),
      );

      batchResults.forEach((result, index) => {
        const expense = batch[index];
        if (!expense) return;

        if (result.status === "fulfilled") {
          results.push({ success: true, expense });
        } else {
          results.push({
            success: false,
            expense,
            error: result.reason,
          });
        }
      });
    }

    return results;
  }

  async function update() {
    await getData();
  }

  function toJson() {
    return JSON.parse(
      JSON.stringify({
        accountMappings: accountMappings.value,
        paymentEntries: paymentEntries.value,
        stockDetails: stockDetails.value,
        currentPeriod: currentPeriod.value,
      }),
    );
  }

  return {
    currentPeriod,
    lastRefresh,
    dateRange,
    accountMappings,
    paymentEntries,
    stockDetails,
    loading,
    update,
    toJson,
    addDraftExpense,
    bulkAddDraftExpenses,
  };
});
