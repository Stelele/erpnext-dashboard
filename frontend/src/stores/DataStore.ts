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

  const currentPeriod = ref<Period>("This Month");
  const lastRefresh = ref("");
  const dateRange = computed(() => getPeriodDateRange(currentPeriod.value));

  async function getData(period: Period) {
    loading.value = true;

    const erpNextService = new ErpNextService();
    const prevPeriod = getPreviousPeriod(period);

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

    loading.value = false;
    setTimeout(
      async () => {
        await getData(currentPeriod.value);
      },
      5 * 60 * 1000,
    );
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
    loading,
    getData,
    addDraftExpense,
  };
});
