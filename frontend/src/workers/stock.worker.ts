import type { GroupSummary } from "../types/MonthSales";
import type {
  StockDetail,
  StockValueSummary,
  DailyStockValue,
} from "../types/StockDetail";
import type { Period } from "../utils/PeriodUtilities";
import type { DoughnutChartData, BarChartData } from "../types/ChartData";
import moment from "moment";
import type { LineChartData } from "@/components/CardLineChart.vue";

moment.updateLocale("en", {
  week: {
    dow: 1, // Monday is the first day of the week
  },
});

interface SerializedData {
  stockValues: StockValueSummary[];
  salesStockValues: GroupSummary[];
  currentPeriod: Period;
  stockDetails: StockDetail[];
  dailyStockValues: DailyStockValue[];
}

export interface StockRow {
  item_code: string;
  item_name: string;
  real_qty: number;
  item_group: string;
  selling_price: number;
  buying_price: number;
  pack_size?: string;
  gross_profit: number;
  total_gross_profit: number;
}

export interface BubbleDataPoint {
  x: number;
  y: number;
  r: number;
  date: string;
}

export interface StockResults {
  salesVsStock: {
    datasets: { label: string; data: BubbleDataPoint[] }[];
    labels: { x: string; y: string };
    additionalData: Record<string, string>[][];
    tooltipLabels: { x: string; y: string }[];
    title: string;
  };
  stockByItemGroup: DoughnutChartData;
  totalStockValue: number;
  totalSellingValue: number;
  averageMarkupPercentage: number;
  stockTableData: StockRow[];
  dailyStockValues: BarChartData;
}

function getFormatedDate(date: string, period: Period) {
  if (["Today", "This Week"].includes(period)) {
    return moment(date, "YYYY-MM-DD").format("DD MMM");
  }

  return moment(date, "YYYY-MM").format("MMM YYYY");
}

function computeStock(data: SerializedData): StockResults {
  const salesVsStock = computeSalesVsStock(data);
  const stockByItemGroup = computeStockByItemGroup(data);
  const totalStockValue = computeTotalStockValue(data);
  const totalSellingValue = computeTotalSellingValue(data);
  const averageMarkupPercentage = computeAverageMarkupPercentage(
    totalStockValue,
    totalSellingValue,
  );
  const stockTableData = computeStockTableData(data);
  const dailyStockValues = computeDailyStockValues(data);

  return {
    salesVsStock,
    stockByItemGroup,
    totalStockValue,
    totalSellingValue,
    averageMarkupPercentage,
    stockTableData,
    dailyStockValues,
  };
}

function computeSalesVsStock(
  data: SerializedData,
): StockResults["salesVsStock"] {
  const groupedSales = data.salesStockValues;
  const groupedStock = data.stockValues;

  const dataEntry = groupedStock.map((stock) => {
    const sales = groupedSales.find(
      (sale) => sale.grouping_name === stock.grouping_name,
    );

    return {
      x: Math.round(stock.average_stock_value),
      y: Math.round(sales?.total ?? 0),
      r: Math.round(sales?.total ?? 0),
      date: getFormatedDate(stock.grouping_name, data.currentPeriod),
    };
  });

  const datasets = [
    {
      label: ["Today", "This Week"].includes(data.currentPeriod)
        ? "Daily Summary"
        : "Monthly Summary",
      data: dataEntry,
    },
  ];

  const additionalData = [
    dataEntry.map((entry) => {
      if (["Today", "This Week"].includes(data.currentPeriod)) {
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

function computeStockByItemGroup(data: SerializedData): DoughnutChartData {
  const categories: Record<string, number> = {};
  for (const summary of data.stockDetails) {
    categories[summary.item_group] =
      (categories[summary.item_group] ?? 0) +
      Math.round(summary.buying_price * summary.real_qty * 100) / 100;
  }

  const labels = Object.keys(categories).sort();
  const dataEntry = labels.map((label) => categories[label] ?? 0);

  return {
    labels,
    datasets: [
      {
        label: "Order Value",
        data: dataEntry,
      },
    ],
  };
}

function computeTotalStockValue(data: SerializedData): number {
  const total = data.stockDetails.reduce(
    (acc, summary) =>
      acc + Math.round(summary.buying_price * summary.real_qty * 100) / 100,
    0,
  );
  return Math.round(total);
}

function computeTotalSellingValue(data: SerializedData): number {
  const total = data.stockDetails.reduce(
    (acc, summary) =>
      acc + Math.round(summary.selling_price * summary.real_qty * 100) / 100,
    0,
  );
  return Math.round(total);
}

function computeAverageMarkupPercentage(
  totalStock: number,
  totalSelling: number,
): number {
  return (
    Math.round(((totalSelling - totalStock) / totalStock) * 100 * 100) / 100
  );
}

function computeStockTableData(data: SerializedData): StockRow[] {
  const entries = data.stockDetails.map((entry) => {
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

function computeDailyStockValues(data: SerializedData) {
  const sortedData = data.dailyStockValues.sort(
    (a, b) => b.days_from_end - a.days_from_end,
  );

  const labels = sortedData.map((data) =>
    moment(data.posting_date, "YYYY-MM-DD").format("DD MMM"),
  );
  const datasets = [
    {
      label: "Closing Stock",
      data: sortedData.map((data) => data.daily_stock_value),
    },
  ];

  return { labels, datasets } as LineChartData;
}

self.onmessage = (e: MessageEvent<SerializedData>) => {
  const results = computeStock(e.data);
  self.postMessage(results);
};
