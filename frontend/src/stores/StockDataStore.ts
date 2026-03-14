import { defineStore } from "pinia";
import type { CardBubbleChartProps } from "@/components/CardBubbleChart.vue";
import { ref } from "vue";
import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import type { StockRow } from "@/components/StockTable.vue";
import type { LineChartData } from "@/components/CardLineChart.vue";
import type { StockResults } from "../workers/stock.worker";

export const useStockDataStore = defineStore("stockDataStore", () => {
  const salesVsStock = ref<CardBubbleChartProps>({
    datasets: [],
    labels: { x: "", y: "" },
    additionalData: [],
    tooltipLabels: [],
    title: "Sales vs Stock",
  });
  const stockByItemGroup = ref<DoughnutChartData>({ labels: [], datasets: [] });
  const totalStockValue = ref(0);
  const totalSellingValue = ref(0);
  const averageMarkupPercentage = ref(0);
  const stockTableData = ref<StockRow[]>([]);
  const dailyStockValues = ref<LineChartData>({ labels: [], datasets: [] });

  function applyResults(results: StockResults) {
    salesVsStock.value = results.salesVsStock;
    stockByItemGroup.value = results.stockByItemGroup;
    totalStockValue.value = results.totalStockValue;
    totalSellingValue.value = results.totalSellingValue;
    averageMarkupPercentage.value = results.averageMarkupPercentage;
    stockTableData.value = results.stockTableData;
    dailyStockValues.value = results.dailyStockValues;
  }

  return {
    salesVsStock,
    stockByItemGroup,
    totalStockValue,
    totalSellingValue,
    averageMarkupPercentage,
    stockTableData,
    dailyStockValues,
    applyResults,
  };
});
