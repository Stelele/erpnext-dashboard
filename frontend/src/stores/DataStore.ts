import { defineStore } from "pinia";
import { ErpNextService } from "../services/ErpNextService";
import { computed, ref } from "vue";
import { getPeriodDateRange, type Period } from "../utils/PeriodUtilities";
import moment from "moment";
import type { Expense, Payment, CompanyExpenseMapping, CompanySettings, AccountMappings } from "../types/Expenses";
import type { StockDetail } from "@/types/StockDetail";
import { fetchAllData } from "@/services/DataFetcherFunctions";
import * as ExpenseServiceFunctions from "@/services/ExpenseServiceFunctions";
import { useOverViewDataStore } from "./OverViewDataStore";
import { useExpenseDataStore } from "./ExpenseDataStore";
import { useStockDataStore } from "./StockDataStore";
import { useSalesDataStore } from "./SalesDataStore";
import { ApiSingleton } from "@/services/api";

export const useDataStore = defineStore("dataStore", () => {
  const overviewStore = useOverViewDataStore();
  const expenseStore = useExpenseDataStore();
  const stockStore = useStockDataStore();
  const salesStore = useSalesDataStore();

  const loading = ref(true);
  const accountMappings = ref<AccountMappings>({
    income: null,
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
    expenseStore.applyExpenseBreakdown(result.expenseBreakdownData, result.expenseMappings);
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

  async function getCompanyExpenseMappings(companyId: string): Promise<CompanyExpenseMapping[]> {
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.GET("/api/companies/{companyId}/expense-mappings", {
      params: { path: { companyId } },
    });
    if (error) return [];
    return data ?? [];
  }

  async function getCompanySettings(companyId: string): Promise<CompanySettings | null> {
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.GET("/api/companies/{companyId}/settings", {
      params: { path: { companyId } },
    });
    if (error) return null;
    return data ?? null;
  }

  async function initAccountMappings(
    expenseMappings: CompanyExpenseMapping[],
    incomeAccountName: string,
  ) {
    const erpNextService = new ErpNextService();
    accountMappings.value = await erpNextService.getAccountMappings(
      expenseMappings,
      incomeAccountName,
    );
  }

  function addDraftExpense(expense: Expense) {
    return ExpenseServiceFunctions.submitExpense(
      new ErpNextService(),
      accountMappings.value,
      expense,
    );
  }

  async function bulkAddDraftExpenses(
    expenses: Expense[],
  ): Promise<ExpenseServiceFunctions.ExpenseSubmissionResult[]> {
    return ExpenseServiceFunctions.bulkSubmitExpenses(
      new ErpNextService(),
      accountMappings.value,
      expenses,
    );
  }

  async function clear() {
    paymentEntries.value = [];
    stockDetails.value = [];
    accountMappings.value = { income: null, expenses: {} } as AccountMappings;

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
    getCompanyExpenseMappings,
    getCompanySettings,
    initAccountMappings,
  };
});
