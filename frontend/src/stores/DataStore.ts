import { defineStore } from "pinia";
import {
  ErpNextService,
  type AccountMappings,
} from "../services/ErpNextService";
import { computed, ref } from "vue";
import {
  getPeriodDateRange,
  getPreviousPeriod,
  type Period,
} from "../utils/PeriodUtilities";
import type { GroupSummary, ItemGroupSummary } from "../types/MonthSales";
import moment from "moment";
import type { Expense, Payment } from "../types/Expenses";
import type { SalesDetail } from "@/types/SalesDetail";
import type { StockDetail, StockValueSummary } from "@/types/StockDetail";

export const useDataStore = defineStore("dataStore", () => {
  const loading = ref(true);
  const salesSummary = ref<GroupSummary[]>([]);
  const prevSalesSummary = ref<GroupSummary[] | undefined>(undefined);
  const prev6MonthsSales = ref<GroupSummary[]>([]);
  const prevXGroupingSales = ref<GroupSummary[]>([]);
  const itemGroupSalesSummary = ref<ItemGroupSummary[]>([]);
  const purchaseGroupSummary = ref<GroupSummary[]>([]);
  const prevPurchaseGroupSummary = ref<GroupSummary[] | undefined>(undefined);
  const expensesSummary = ref<GroupSummary[]>([]);
  const prevExpensesSummary = ref<GroupSummary[] | undefined>(undefined);
  const accountMappings = ref<AccountMappings>({
    incomes: {},
    expenses: {},
  } as AccountMappings);
  const paymentEntries = ref<Payment[]>([]);
  const sales = ref<SalesDetail[]>([]);
  const stockDetails = ref<StockDetail[]>([]);

  const stockValues = ref<StockValueSummary[]>([]);
  const salesStockValues = ref<GroupSummary[]>([]);

  const currentPeriod = ref<Period>("This Month");
  const lastRefresh = ref("");
  const dateRange = computed(() => getPeriodDateRange(currentPeriod.value));

  async function getData() {
    loading.value = true;

    const erpNextService = new ErpNextService();
    const period = currentPeriod.value;
    const prevPeriod = getPreviousPeriod(currentPeriod.value);

    lastRefresh.value = moment().format("DD-MMM-YY HH:mm");
    const erpNextServicePromises: [
      Promise<GroupSummary[]>,
      Promise<GroupSummary[] | undefined>,
      Promise<GroupSummary[]>,
      Promise<GroupSummary[]>,
      Promise<ItemGroupSummary[]>,
      Promise<GroupSummary[]>,
      Promise<GroupSummary[] | undefined>,
      Promise<GroupSummary[]>,
      Promise<GroupSummary[] | undefined>,
      Promise<AccountMappings>,
      Promise<Payment[]>,
      Promise<SalesDetail[]>,
      Promise<StockDetail[]>,
      Promise<StockValueSummary[]>,
      Promise<GroupSummary[]>,
    ] = [
      erpNextService.getSalesSummary(period),
      prevPeriod
        ? erpNextService.getSalesSummary(prevPeriod)
        : new Promise((resolve) => resolve(undefined)),
      erpNextService.getPrevGroupedSales("months", 6),
      erpNextService.getPrevGroupSalesFromCurrent(period),
      erpNextService.getItemGroupSalesSummary(period),
      erpNextService.getPurchaseGroupSummary(period),
      prevPeriod
        ? erpNextService.getPurchaseGroupSummary(prevPeriod)
        : new Promise((resolve) => resolve(undefined)),
      erpNextService.getExpensesSummary(period),
      prevPeriod
        ? erpNextService.getExpensesSummary(prevPeriod)
        : new Promise((resolve) => resolve(undefined)),
      erpNextService.getAccountMappings(),
      erpNextService.getPaymentEntries(period),
      erpNextService.getSales(period),
      erpNextService.getStockLevels(),
      Promise.all([
        erpNextService.getStockValueSummary(
          ["Today", "This Week"].includes(period)
            ? "This Week"
            : "This Semester",
        ),
        erpNextService.getStockValueSummary(
          ["Yesterday", "Last Week"].includes(prevPeriod ?? "")
            ? "Last Week"
            : "Last Semester",
        ),
      ]).then((r) => r.flat()),
      Promise.all([
        erpNextService.getSalesSummary(
          ["Today", "This Week"].includes(period)
            ? "This Week"
            : "This Semester",
        ),
        erpNextService.getSalesSummary(
          ["Yesterday", "Last Week"].includes(prevPeriod ?? "")
            ? "Last Week"
            : "Last Semester",
        ),
      ]).then((r) => r.flat()),
    ];

    const result = await Promise.all(erpNextServicePromises);
    salesSummary.value = result[0];
    prevSalesSummary.value = result[1];
    prev6MonthsSales.value = result[2];
    prevXGroupingSales.value = result[3];
    itemGroupSalesSummary.value = result[4];
    purchaseGroupSummary.value = result[5];
    prevPurchaseGroupSummary.value = result[6];
    expensesSummary.value = result[7];
    prevExpensesSummary.value = result[8];
    accountMappings.value = result[9];
    paymentEntries.value = result[10];
    sales.value = result[11];
    stockDetails.value = result[12];
    stockValues.value = result[13];
    salesStockValues.value = result[14];

    loading.value = false;
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

  async function update() {
    await getData();
  }

  return {
    currentPeriod,
    lastRefresh,
    dateRange,
    salesSummary,
    prevSalesSummary,
    prev6MonthsSales,
    prevXGroupingSales,
    itemGroupSalesSummary,
    purchaseGroupSummary,
    prevPurchaseGroupSummary,
    expensesSummary,
    prevExpensesSummary,
    accountMappings,
    paymentEntries,
    stockDetails,
    stockValues,
    salesStockValues,
    sales,
    loading,
    update,
    addDraftExpense,
  };
});
