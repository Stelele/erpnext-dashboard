import { defineStore } from "pinia";
import { useDataStore } from "./DataStore";
import type {
  CardBubbleChartProps,
  BubbleDataset,
} from "@/components/CardBubbleChart.vue";
import { ref } from "vue";
import type { Period } from "@/utils/PeriodUtilities";
import moment from "moment";
import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import type { StockRow } from "@/components/StockTable.vue";

export const useStockDataStore = defineStore("stockDataStore", () => {
  const dataStore = useDataStore();

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

  function update() {
    salesVsStock.value = getSalesVsStock();
    stockByItemGroup.value = getStockByItemGroup();
    totalStockValue.value = getTotalStockValue();
    totalSellingValue.value = getTotalSellingValue();
    averageMarkupPercentage.value = getAverageMarkupPercentage();
    stockTableData.value = getStockTableData();

    function getSalesVsStock() {
      const groupedSales = dataStore.salesStockValues;
      const groupedStock = dataStore.stockValues;

      const data = groupedStock.map((stock) => {
        const sales = groupedSales.find(
          (sale) => sale.grouping_name === stock.grouping_name,
        );

        return {
          x: Math.round(stock.average_stock_value),
          y: Math.round(sales?.total ?? 0),
          r: Math.round(sales?.total ?? 0),
          date: getFormatedDate(stock.grouping_name, dataStore.currentPeriod),
        };
      });

      const datasets: BubbleDataset[] = [
        {
          label: ["Today", "This Week"].includes(dataStore.currentPeriod)
            ? "Daily Summary"
            : "Monthly Summary",
          data,
        },
      ];

      const additionalData = [
        data.map((entry) => {
          if (["Today", "This Week"].includes(dataStore.currentPeriod)) {
            return { Date: entry.date } as Record<string, string>;
          }

          return { Month: entry.date } as Record<string, string>;
        }),
      ];
      return {
        datasets,
        additionalData,
        labels: {
          x: "Average Stock Value ($)",
          y: "Total Sales ($)",
        },
        tooltipLabels: [{ x: "Avg. Stock ($)", y: "Total Sales ($)" }],
        title: "Sales vs Stock",
      };
    }

    function getStockByItemGroup() {
      const categories: Record<string, number> = {};
      for (const summary of dataStore.stockDetails) {
        categories[summary.item_group] =
          (categories[summary.item_group] ?? 0) +
          Math.round(summary.buying_price * summary.real_qty * 100) / 100;
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

    function getTotalStockValue() {
      const total = dataStore.stockDetails.reduce(
        (acc, summary) =>
          acc + Math.round(summary.buying_price * summary.real_qty * 100) / 100,
        0,
      );
      return Math.round(total);
    }

    function getTotalSellingValue() {
      const total = dataStore.stockDetails.reduce(
        (acc, summary) =>
          acc +
          Math.round(summary.selling_price * summary.real_qty * 100) / 100,
        0,
      );
      return Math.round(total);
    }

    function getAverageMarkupPercentage() {
      const totalStock = totalStockValue.value;
      const totalSelling = totalSellingValue.value;
      return (
        Math.round(((totalSelling - totalStock) / totalStock) * 100 * 100) / 100
      );
    }

    function getStockTableData() {
      const entries = dataStore.stockDetails.map((entry) => {
        return {
          ...entry,
          gross_profit: entry.selling_price - entry.buying_price,
          total_gross_profit:
            (entry.selling_price - entry.buying_price) * entry.real_qty,
        } as StockRow;
      });

      return entries.sort((a, b) => {
        if (a.item_group.localeCompare(b.item_group) === 0) {
          return a.item_name.localeCompare(b.item_name);
        }

        return a.item_group.localeCompare(b.item_group);
      });
    }
  }

  return {
    salesVsStock,
    stockByItemGroup,
    totalStockValue,
    totalSellingValue,
    averageMarkupPercentage,
    stockTableData,
    update,
  };
});

function getFormatedDate(date: string, period: Period) {
  if (["Today", "This Week"].includes(period)) {
    return moment(date, "YYYY-MM-DD").format("DD MMM");
  }

  return moment(date, "YYYY-MM").format("MMM YYYY");
}
