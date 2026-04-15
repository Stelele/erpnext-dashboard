import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import moment from "moment";
import type { BarChartData } from "@/components/CardBarChart.vue";
import { ExpenseAccountMapping, type ExpenseType } from "@/types/Expenses";
import type { AccountResponse } from "@/services/ErpNextService";

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

export const useExpenseDataStore = defineStore("ExpenseDataStore", () => {
  const expenseBreakdown = ref<DoughnutChartData>({
    labels: [],
    datasets: [],
  });

  const orderBreakdown = ref<DoughnutChartData>({
    labels: [],
    datasets: [],
  });
  const prev6MonthsExpenses = ref<BarChartData>({ labels: [], datasets: [] });

  function applyExpenseBreakdown(
    data: ExpenseTypeData[],
    accountMappings: Record<ExpenseType, AccountResponse>
  ) {
    const mapped: Record<ExpenseType, number> = {
      Sekuru: 0,
      Canteen: 0,
      "Spoiled Meat": 0,
      Utilities: 0,
      Consumables: 0,
      "Neg Variance": 0,
      Staff: 0,
      Other: 0,
    };

    for (const d of data) {
      const expenseAccounts = Object.values(accountMappings);
      const retrievedAccount = expenseAccounts.find((ea) => ea.name === d.expense_type);
      const friendlyType = (
        Object.entries(ExpenseAccountMapping) as [ExpenseType, string][]
      ).find(([_, accountName]) => accountName === retrievedAccount?.account_name)?.[0] ?? "Other";
      mapped[friendlyType] = (mapped[friendlyType] ?? 0) + d.total;
    }

    const labels = Object.keys(mapped).filter(k => mapped[k as ExpenseType] > 0) as ExpenseType[];
    labels.sort((a, b) => mapped[b as ExpenseType] - mapped[a as ExpenseType]);
    const values = labels.map(k => mapped[k]);

    expenseBreakdown.value = {
      labels,
      datasets: [{
        label: "Expense Value",
        data: values,
      }],
    };
  }

  function applyOrderBreakdown(data: OrderTypeData[]) {
    orderBreakdown.value = {
      labels: data.map(d => d.description),
      datasets: [{
        label: "Order Value",
        data: data.map(d => d.total),
      }],
    };
  }

  function applyPrev6MonthsExpenses(data: DashboardInterval[]) {
    prev6MonthsExpenses.value = {
      labels: data.map(d => moment(d.grouping_name, "YYYY-MM").format("MMM YY")),
      datasets: [{
        label: "Expenses",
        data: data.map(d => d.total),
      }],
    };
  }

  function parseDashboardResults(results: any[]) {
    const prevExpenseData: DashboardInterval[] = [];

    results.forEach(row => {
      if (row.metric_type === "expenses_by_month") {
        prevExpenseData.push(row);
      }
    });

    if (prevExpenseData.length > 0) {
      applyPrev6MonthsExpenses(prevExpenseData);
    }
  }

  return {
    expenseBreakdown,
    orderBreakdown,
    prev6MonthsExpenses,
    applyExpenseBreakdown,
    applyOrderBreakdown,
    applyPrev6MonthsExpenses,
    parseDashboardResults,
  };
});
