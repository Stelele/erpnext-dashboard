import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { useDataStore } from "./DataStore";
import type { BarChartData } from "@/components/CardBarChart.vue";
import { ExpenseAccountMapping } from "@/types/Expenses";
import moment from "moment";

export const useExpenseDataStore = defineStore("ExpenseDataStore", () => {
  const dataStore = useDataStore();

  const expenseBreakdown = ref<DoughnutChartData>({
    labels: [],
    datasets: [],
  });

  const orderBreakdown = ref<DoughnutChartData>({
    labels: [],
    datasets: [],
  });
  const prev6MonthsExpenses = ref<BarChartData>({ labels: [], datasets: [] });

  function update() {
    expenseBreakdown.value = getExpenseBreakdown();
    orderBreakdown.value = getOrderBreakdown();
    prev6MonthsExpenses.value = getPrev6MonthsExpenses();

    function getExpenseBreakdown() {
      const expenses = dataStore.paymentEntries.filter(
        (pe) => pe.type === "Expense",
      );
      const categories: Record<string, number> = {};
      for (const expense of expenses) {
        const expenseType = getExpenseType(expense.account ?? "");
        categories[expenseType] =
          (categories[expenseType] ?? 0) + expense.amount;
      }

      const labels = Object.keys(categories).sort();
      const data = labels.map((label) => categories[label] ?? 0);

      return {
        labels,
        datasets: [
          {
            label: "Expense Value",
            data,
          },
        ],
      };
    }

    function getExpenseType(account: string) {
      const expenseAccounts = Object.values(dataStore.accountMappings.expenses);
      const retrievedAccount = expenseAccounts.find(
        (ea) => ea.name === account,
      );
      return (
        Object.entries(ExpenseAccountMapping).find(
          ([_, value]) => value === retrievedAccount?.account_name,
        )?.[0] ?? "Unknown"
      );
    }

    function getPrev6MonthsExpenses() {
      const labels: string[] = [];
      const data: number[] = [];

      for (let i = 5; i >= 0; i--) {
        const month = moment().subtract(i, "months").format("YYYY-MM");
        const formattedMonth = moment(month, "YYYY-MM").format("MMM YYYY");
        labels.push(formattedMonth);
        const monthExpenses = dataStore.prev6MonthsExpenses.find(
          (monthSale) => monthSale.grouping_name === month,
        );
        data.push(monthExpenses?.total ?? 0);
      }

      return {
        labels,
        datasets: [
          {
            label: "Sales",
            data,
          },
        ],
      };
    }

    function getOrderBreakdown() {
      const orders = dataStore.paymentEntries.filter(
        (pe) => pe.type === "Order",
      );
      const categories: Record<string, number> = {};
      for (const order of orders) {
        categories[order.description] =
          (categories[order.description] ?? 0) + order.amount;
      }

      const labels = Object.keys(categories).sort();
      const data = labels.map((label) => categories[label] ?? 0);

      return {
        labels,
        datasets: [
          {
            label: "Order Value",
            data,
          },
        ],
      };
    }
  }

  return {
    expenseBreakdown,
    orderBreakdown,
    prev6MonthsExpenses,
    update,
  };
});
