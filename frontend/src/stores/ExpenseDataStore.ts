import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import type { BarChartData } from "@/components/CardBarChart.vue";

interface ExpenseTypeData {
  expense_type: string;
  total: number;
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

  function applyExpenseBreakdown(data: ExpenseTypeData[]) {
    expenseBreakdown.value = {
      labels: data.map(d => d.expense_type),
      datasets: [{
        label: "Expense Value",
        data: data.map(d => d.total),
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
      labels: data.map(d => d.grouping_name),
      datasets: [{
        label: "Expenses",
        data: data.map(d => d.total),
      }],
    };
  }

  function parseDashboardResults(results: any[]) {
    const expenseTypeData: ExpenseTypeData[] = [];
    const prevExpenseData: DashboardInterval[] = [];

    results.forEach(row => {
      switch (row.metric_type) {
        case "expenses_by_type":
          expenseTypeData.push(row);
          break;
        case "expenses_by_month":
          prevExpenseData.push(row);
          break;
      }
    });

    if (expenseTypeData.length > 0) {
      applyExpenseBreakdown(expenseTypeData);
    }
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
