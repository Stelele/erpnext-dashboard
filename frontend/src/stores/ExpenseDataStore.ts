import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import moment from "moment";
import type { BarChartData } from "@/components/CardBarChart.vue";
import type { AccountResponse, CompanyExpenseMapping } from "@/types/Expenses";

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
    expenseMappings: CompanyExpenseMapping[]
  ) {
    const mapped: Record<string, number> = {};

    for (const d of data) {
      const mapping = expenseMappings.find(
        (m) => m.erpnextAccountName === d.expense_type
      );
      if (mapping) {
        const name = mapping.expenseTypeName;
        mapped[name] = (mapped[name] ?? 0) + d.total;
      }
    }

    const labels = Object.keys(mapped).filter(k => mapped[k] > 0);
    labels.sort((a, b) => mapped[b] - mapped[a]);
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

  function clear() {
    expenseBreakdown.value = { labels: [], datasets: [] };
    orderBreakdown.value = { labels: [], datasets: [] };
    prev6MonthsExpenses.value = { labels: [], datasets: [] };
  }

  return {
    expenseBreakdown,
    orderBreakdown,
    prev6MonthsExpenses,
    applyExpenseBreakdown,
    applyOrderBreakdown,
    applyPrev6MonthsExpenses,
    parseDashboardResults,
    clear,
  };
});
