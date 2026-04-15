import { defineStore } from "pinia";
import type { CardBubbleChartProps } from "@/components/CardBubbleChart.vue";
import { ref } from "vue";
import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import type { StockRow } from "@/components/StockTable.vue";
import type { LineChartData } from "@/components/CardLineChart.vue";
import type { StockDetail } from "@/types/StockDetail";

interface StockGroupData {
  item_group: string;
  total: number;
}

interface StockSummaryData {
  total_value: number;
  selling_value: number;
}

interface BubbleDataPoint {
  x: number;
  y: number;
  r: number;
  date: string;
}

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

  function applyStockByItemGroup(data: StockGroupData[]) {
    stockByItemGroup.value = {
      labels: data.map(d => d.item_group),
      datasets: [{
        label: "Order Value",
        data: data.map(d => d.total),
      }],
    };
  }

  function applyStockSummary(data: StockSummaryData) {
    totalStockValue.value = data.total_value || 0;
    totalSellingValue.value = data.selling_value || 0;
    if (data.total_value > 0) {
      averageMarkupPercentage.value = Math.round(((data.selling_value - data.total_value) / data.total_value) * 100 * 100) / 100;
    }
  }

  function applySalesVsStock(
    stockData: { grouping_name: string; average_stock_value: number }[],
    salesData: { grouping_name: string; total: number }[],
    currentPeriod: string
  ) {
    const dataEntry: BubbleDataPoint[] = stockData.map((stock) => {
      const sales = salesData.find(s => s.grouping_name === stock.grouping_name);
      return {
        x: Math.round(stock.average_stock_value),
        y: Math.round(sales?.total ?? 0),
        r: Math.round(sales?.total ?? 0),
        date: formatDate(stock.grouping_name, currentPeriod),
      };
    });

    salesVsStock.value = {
      datasets: [{
        label: ["Today", "This Week"].includes(currentPeriod) ? "Daily Summary" : "Monthly Summary",
        data: dataEntry,
      }],
      additionalData: [dataEntry.map(entry => {
        if (["Today", "This Week"].includes(currentPeriod)) {
          return { Date: entry.date } as Record<string, string>;
        }
        return { Month: entry.date } as Record<string, string>;
      })],
      labels: { x: "Average Stock Value ($)", y: "Total Sales ($)" },
      tooltipLabels: [{ x: "Avg. Stock ($)", y: "Total Sales ($)" }],
      title: "Sales vs Stock",
    };
  }

  function formatDate(dateStr: string, _period: string): string {
    if (!dateStr || !dateStr.includes("-")) {
      return dateStr;
    }
    if (dateStr.length === 10) {
      const date = new Date(dateStr);
      return date.toLocaleDateString("en-US", { day: "2-digit", month: "short" });
    } else if (dateStr.length === 7) {
      const parts = dateStr.split("-");
      const year = parseInt(parts[0] || "0");
      const month = parseInt(parts[1] || "0") - 1;
      const date = new Date(year, month);
      return date.toLocaleDateString("en-US", { month: "short", year: "numeric" });
    }
    return dateStr;
  }

  function setStockTableData(data: StockDetail[]) {
    stockTableData.value = data.map(entry => ({
      ...entry,
      gross_profit: entry.selling_price - entry.buying_price,
      total_gross_profit: (entry.selling_price - entry.buying_price) * entry.real_qty,
    }));
  }

  function setDailyStockValues(data: LineChartData) {
    dailyStockValues.value = data;
  }

  function parseDashboardResults(results: any[]) {
    const stockGroupData: StockGroupData[] = [];
    let stockSummaryData: StockSummaryData = { total_value: 0, selling_value: 0 };

    results.forEach(row => {
      switch (row.metric_type) {
        case "stock_by_group":
          stockGroupData.push(row);
          break;
        case "stock_summary":
          stockSummaryData = row;
          break;
      }
    });

    if (stockGroupData.length > 0) {
      applyStockByItemGroup(stockGroupData);
    }
    applyStockSummary(stockSummaryData);
  }

  return {
    salesVsStock,
    stockByItemGroup,
    totalStockValue,
    totalSellingValue,
    averageMarkupPercentage,
    stockTableData,
    dailyStockValues,
    applyStockByItemGroup,
    applyStockSummary,
    applySalesVsStock,
    setStockTableData,
    setDailyStockValues,
    parseDashboardResults,
  };
});
