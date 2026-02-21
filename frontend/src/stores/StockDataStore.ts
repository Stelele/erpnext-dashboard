import { defineStore } from "pinia";
import { useDataStore } from "./DataStore";
import type {
  CardBubbleChartProps,
  BubbleDataset,
} from "@/components/CardBubbleChart.vue";
import { computed } from "vue";
import type { Period } from "@/utils/PeriodUtilities";
import moment from "moment";

export const useStockDataStore = defineStore("stockDataStore", () => {
  const dataStore = useDataStore();

  const salesVsStock = computed<CardBubbleChartProps>(() => {
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
  });

  const stockByItemGroup = computed(() => {
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
  });

  const totalStockValue = computed(() => {
    const total = dataStore.stockDetails.reduce(
      (acc, summary) =>
        acc + Math.round(summary.buying_price * summary.real_qty * 100) / 100,
      0,
    );
    return Math.round(total);
  });

  const totalSellingValue = computed(() => {
    const total = dataStore.stockDetails.reduce(
      (acc, summary) =>
        acc + Math.round(summary.selling_price * summary.real_qty * 100) / 100,
      0,
    );
    return Math.round(total);
  });

  const averageMarkupPercentage = computed(() => {
    const totalStock = totalStockValue.value;
    const totalSelling = totalSellingValue.value;
    return (
      Math.round(((totalSelling - totalStock) / totalStock) * 100 * 100) / 100
    );
  });

  return {
    salesVsStock,
    stockByItemGroup,
    totalStockValue,
    totalSellingValue,
    averageMarkupPercentage,
  };
});

function getFormatedDate(date: string, period: Period) {
  if (["Today", "This Week"].includes(period)) {
    return moment(date, "YYYY-MM-DD").format("DD MMM");
  }

  return moment(date, "YYYY-MM").format("MMM YYYY");
}
