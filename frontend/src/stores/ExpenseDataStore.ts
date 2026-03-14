import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import type { BarChartData } from "@/components/CardBarChart.vue";
import type { ExpenseResults } from "../workers/expense.worker";

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

  function applyResults(results: ExpenseResults) {
    expenseBreakdown.value = results.expenseBreakdown;
    orderBreakdown.value = results.orderBreakdown;
    prev6MonthsExpenses.value = results.prev6MonthsExpenses;
  }

  return {
    expenseBreakdown,
    orderBreakdown,
    prev6MonthsExpenses,
    applyResults,
  };
});
