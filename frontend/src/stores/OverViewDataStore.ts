import { defineStore } from "pinia";
import { ref } from "vue";
import type { CardData } from "../types/CardData";
import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import type { BarChartData } from "@/components/CardBarChart.vue";
import type { OverviewResults } from "../workers/overview.worker";

export const useOverViewDataStore = defineStore("overViewDataStore", () => {
  const nrSales = ref<CardData>({ value: 0 });
  const totalSales = ref<CardData>({ value: 0 });
  const avgSales = ref<CardData>({ value: 0 });
  const nrPurchases = ref<CardData>({ value: 0 });
  const totalPurchases = ref<CardData>({ value: 0 });
  const avgPurchases = ref<CardData>({ value: 0 });
  const grossProfit = ref<CardData>({ value: 0 });
  const grossMargin = ref<CardData>({ value: 0 });
  const nrExpenses = ref<CardData>({ value: 0 });
  const totalExpenses = ref<CardData>({ value: 0 });
  const avgExpenses = ref<CardData>({ value: 0 });
  const netProfit = ref<CardData>({ value: 0 });
  const netMargin = ref<CardData>({ value: 0 });
  const salesByCategory = ref<DoughnutChartData>({ labels: [], datasets: [] });
  const prevXGroupingSales = ref<{ data: BarChartData; title: string }>({
    title: "",
    data: { labels: [], datasets: [] },
  });
  const prev6MonthsSales = ref<BarChartData>({ labels: [], datasets: [] });

  function applyResults(results: OverviewResults) {
    nrSales.value = results.nrSales;
    totalSales.value = results.totalSales;
    avgSales.value = results.avgSales;
    nrPurchases.value = results.nrPurchases;
    totalPurchases.value = results.totalPurchases;
    avgPurchases.value = results.avgPurchases;
    grossProfit.value = results.grossProfit;
    grossMargin.value = results.grossMargin;
    nrExpenses.value = results.nrExpenses;
    totalExpenses.value = results.totalExpenses;
    avgExpenses.value = results.avgExpenses;
    netProfit.value = results.netProfit;
    netMargin.value = results.netMargin;
    salesByCategory.value = results.salesByCategory;
    prevXGroupingSales.value = results.prevXGroupingSales;
    prev6MonthsSales.value = results.prev6MonthsSales;
  }

  return {
    nrSales,
    totalSales,
    avgSales,
    nrPurchases,
    totalPurchases,
    avgPurchases,
    grossProfit,
    grossMargin,
    nrExpenses,
    totalExpenses,
    avgExpenses,
    netProfit,
    netMargin,
    salesByCategory,
    prev6MonthsSales,
    prevXGroupingSales,
    applyResults,
  };
});
