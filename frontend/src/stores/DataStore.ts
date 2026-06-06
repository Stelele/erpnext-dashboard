import { defineStore } from "pinia";
import { ErpNextService, type AccountMappings } from "../services/ErpNextService";
import { computed, ref } from "vue";
import { getPeriodDateRange, type Period } from "../utils/PeriodUtilities";
import moment from "moment";
import type { Expense, Payment } from "../types/Expenses";
import type { StockDetail } from "@/types/StockDetail";
import { fetchAllData } from "@/services/DataFetcherFunctions";
import * as ExpenseServiceFunctions from "@/services/ExpenseServiceFunctions";
import { useOverViewDataStore } from "./OverViewDataStore";
import { useExpenseDataStore } from "./ExpenseDataStore";
import { useStockDataStore } from "./StockDataStore";
import { useSalesDataStore } from "./SalesDataStore";

export const useDataStore = defineStore("dataStore", () => {
  const overviewStore = useOverViewDataStore();
  const expenseStore = useExpenseDataStore();
  const stockStore = useStockDataStore();
  const salesStore = useSalesDataStore();

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

    overviewStore.parseDashboardResults(result.dashboardResults);
    overviewStore.applyBarChart(
      { ...result.barChartData, fromDate: result.barChartConfig.fromDate, toDate: result.barChartConfig.toDate },
      result.barChartTitle,
      result.barChartConfig.grouping
    );
    expenseStore.parseDashboardResults(result.dashboardResults);
    expenseStore.applyExpenseBreakdown(result.expenseBreakdownData, result.accountMappings.expenses);
    expenseStore.applyOrderBreakdown(result.orderBreakdownData);
    expenseStore.applyPrev6MonthsExpenses(result.prevExpensesData);
    stockStore.parseDashboardResults(result.dashboardResults);
    stockStore.applySalesVsStock(result.stockValueData, result.salesSummaryData, currentPeriod.value);
    stockStore.setStockTableData(result.stockDetails);
    stockStore.setDailyStockValues({
      labels: result.dailyStockValues.map(d => moment(d.posting_date).format("DD MMM")),
      datasets: [{
        label: "Closing Stock",
        data: result.dailyStockValues.map(d => d.daily_stock_value),
      }],
    });
    salesStore.applyAggregatedSales(result.aggregatedSales);
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

    overviewStore.clear();
    expenseStore.clear();
    stockStore.clear();
    salesStore.clear();
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
