import { defineStore } from "pinia";
import { ErpNextService, type AccountMappings } from "../services/ErpNextService";
import { computed, ref } from "vue";
import { getPeriodDateRange, type Period } from "../utils/PeriodUtilities";
import moment from "moment";
import type { Expense, Payment } from "../types/Expenses";
import type { StockDetail } from "@/types/StockDetail";
import { fetchAllData } from "@/services/DataFetcherFunctions";
import * as ExpenseServiceFunctions from "@/services/ExpenseServiceFunctions";

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

  async function getData() {
    const erpNextService = new ErpNextService();
    const result = await fetchAllData(currentPeriod.value, erpNextService);

    lastRefresh.value = moment().format("DD-MMM-YY HH:mm");

    accountMappings.value = result.accountMappings;
    paymentEntries.value = result.paymentEntries;
    stockDetails.value = result.stockDetails;

    const { useOverViewDataStore } = await import("./OverViewDataStore");
    const { useExpenseDataStore } = await import("./ExpenseDataStore");
    const { useStockDataStore } = await import("./StockDataStore");
    const { useSalesDataStore } = await import("./SalesDataStore");

    useOverViewDataStore().parseDashboardResults(result.dashboardResults);
    useOverViewDataStore().applyBarChart(
      { ...result.barChartData, fromDate: result.barChartConfig.fromDate, toDate: result.barChartConfig.toDate },
      result.barChartTitle,
      result.barChartConfig.grouping
    );
    useExpenseDataStore().parseDashboardResults(result.dashboardResults);
    useExpenseDataStore().applyExpenseBreakdown(result.expenseBreakdownData, result.accountMappings.expenses);
    useExpenseDataStore().applyOrderBreakdown(result.orderBreakdownData);
    useExpenseDataStore().applyPrev6MonthsExpenses(result.prevExpensesData);
    useStockDataStore().parseDashboardResults(result.dashboardResults);
    useStockDataStore().applySalesVsStock(result.stockValueData, result.salesSummaryData, currentPeriod.value);
    useStockDataStore().setStockTableData(result.stockDetails);
    useStockDataStore().setDailyStockValues({
      labels: result.dailyStockValues.map(d => moment(d.posting_date).format("DD MMM")),
      datasets: [{
        label: "Closing Stock",
        data: result.dailyStockValues.map(d => d.daily_stock_value),
      }],
    });
    useSalesDataStore().applyAggregatedSales(result.aggregatedSales);
  }

  function addDraftExpense(expense: Expense) {
    return ExpenseServiceFunctions.addDraftExpense(
      new ErpNextService(),
      accountMappings.value,
      expense,
    );
  }

  async function bulkAddDraftExpenses(
    expenses: Expense[],
  ): Promise<ExpenseServiceFunctions.ExpenseSubmissionResult[]> {
    return ExpenseServiceFunctions.bulkAddDraftExpenses(
      new ErpNextService(),
      accountMappings.value,
      expenses,
    );
  }

  async function clear() {
    paymentEntries.value = [];
    stockDetails.value = [];
    accountMappings.value = { incomes: {}, expenses: {} } as AccountMappings;

    const { useOverViewDataStore } = await import("./OverViewDataStore");
    const { useExpenseDataStore } = await import("./ExpenseDataStore");
    const { useStockDataStore } = await import("./StockDataStore");
    const { useSalesDataStore } = await import("./SalesDataStore");

    useOverViewDataStore().clear();
    useExpenseDataStore().clear();
    useStockDataStore().clear();
    useSalesDataStore().clear();
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
    clear,
    toJson,
    addDraftExpense,
    bulkAddDraftExpenses,
  };
});
