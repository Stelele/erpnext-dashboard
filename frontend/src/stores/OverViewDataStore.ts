import { defineStore } from "pinia";
import { ref } from "vue";
import moment from "moment";
import type { CardData } from "../types/CardData";
import type { DoughnutChartData } from "@/components/CardDoughnutChart.vue";
import type { BarChartData } from "@/components/CardBarChart.vue";

type ChangeDirection = "up" | "down" | "flat";
type BarChartGrouping = "day" | "week" | "month" | "quarter";

interface DashboardMetric {
  cur_total: number;
  cur_count: number;
  prev_total: number;
  prev_count: number;
}

interface DashboardInterval {
  grouping_name: string;
  total: number;
  count: number;
}

interface DashboardCategory {
  item_group: string;
  total: number;
}

function calculatePercentChange(curValue: number, prevValue: number): { percentChange: number; direction: ChangeDirection } {
  if (!prevValue) {
    return { percentChange: 0, direction: "flat" };
  }
  const percentChange = Math.round(((curValue - prevValue) / prevValue) * 100);
  const direction: ChangeDirection =
    percentChange > 0 ? "up" : percentChange < 0 ? "down" : "flat";
  return {
    percentChange: Math.abs(percentChange),
    direction,
  };
}

function createCardData(value: number, prevValue?: number): CardData {
  if (prevValue !== undefined && prevValue !== 0) {
    const change = calculatePercentChange(value, prevValue);
    return {
      value,
      prevValue,
      direction: change.direction,
      percentChange: change.percentChange,
    };
  }
  return { value };
}

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
  const prevXGroupingSalesTitle = ref<string>("");
  const prevXGroupingSales = ref<BarChartData>({ labels: [], datasets: [] });
  const prev6MonthsSales = ref<BarChartData>({ labels: [], datasets: [] });

  function applySalesSummary(data: DashboardMetric) {
    const curTotal = data.cur_total || 0;
    const prevTotal = data.prev_total || 0;
    const curCount = data.cur_count || 0;
    const prevCount = data.prev_count || 0;

    totalSales.value = createCardData(curTotal, prevTotal);
    nrSales.value = createCardData(curCount, prevCount);
    avgSales.value = createCardData(
      curCount > 0 ? curTotal / curCount : 0,
      prevCount > 0 ? prevTotal / prevCount : undefined
    );
  }

  function applyPurchaseSummary(data: DashboardMetric) {
    const curTotal = data.cur_total || 0;
    const prevTotal = data.prev_total || 0;
    const curCount = data.cur_count || 0;
    const prevCount = data.prev_count || 0;

    totalPurchases.value = createCardData(curTotal, prevTotal);
    nrPurchases.value = createCardData(curCount, prevCount);
    avgPurchases.value = createCardData(
      curCount > 0 ? curTotal / curCount : 0,
      prevCount > 0 ? prevTotal / prevCount : undefined
    );
  }

  function applyExpenseSummary(data: DashboardMetric) {
    const curTotal = data.cur_total || 0;
    const prevTotal = data.prev_total || 0;
    const curCount = data.cur_count || 0;
    const prevCount = data.prev_count || 0;

    totalExpenses.value = createCardData(curTotal, prevTotal);
    nrExpenses.value = createCardData(curCount, prevCount);
    avgExpenses.value = createCardData(
      curCount > 0 ? curTotal / curCount : 0,
      prevCount > 0 ? prevTotal / prevCount : undefined
    );
  }



  function formatBarChartLabels(grouping: BarChartGrouping, labels: string[]): string[] {
    return labels.map((label) => {
      if (grouping === "day") {
        return moment(label, "YYYY-MM-DD").format("DD MMM");
      }
      if (grouping === "week") {
        const start = moment(label, "YYYY-MM-DD");
        const end = start.clone().add(6, "days");
        return `${start.format("DD MMM")} - ${end.format("DD MMM")}`;
      }
      if (grouping === "month") {
        return moment(label, "YYYY-MM").format("MMM YY");
      }
      if (grouping === "quarter") {
        const [year, qStr] = label.split("-Q");
        const q = parseInt(qStr, 10);
        const y = parseInt(year, 10);
        const qStart = moment().year(y).quarter(q).startOf("quarter");
        const qEnd = moment().year(y).quarter(q).endOf("quarter");
        return `${qStart.format("MMM YY")} - ${qEnd.format("MMM YY")}`;
      }
      return label;
    });
  }

  function generateExpectedPeriods(
    fromDate: string,
    toDate: string,
    grouping: BarChartGrouping
  ): string[] {
    const periods: string[] = [];
    let current = moment(fromDate);

    if (grouping === "week") {
      current = current.clone().startOf("week");
    } else if (grouping === "month") {
      current = current.clone().startOf("month");
    }

    const end = moment(toDate);

    while (current.isSameOrBefore(end, grouping === "day" ? "day" : grouping)) {
      if (grouping === "day") {
        periods.push(current.format("YYYY-MM-DD"));
        current.add(1, "day");
      } else if (grouping === "week") {
        periods.push(current.format("YYYY-MM-DD"));
        current.add(1, "week");
      } else if (grouping === "month") {
        periods.push(current.format("YYYY-MM"));
        current.add(1, "month");
      } else if (grouping === "quarter") {
        periods.push(`${current.year()}-Q${current.quarter()}`);
        current.add(1, "quarter");
      }
    }

    return periods;
  }

  function fillMissingPeriods(
    labels: string[],
    data: number[],
    grouping: BarChartGrouping,
    fromDate: string,
    toDate: string
  ): { labels: string[]; data: number[] } {
    const expected = generateExpectedPeriods(fromDate, toDate, grouping);
    const dataMap = new Map(labels.map((l, i) => [l, data[i] ?? 0]));

    return {
      labels: expected,
      data: expected.map((p) => dataMap.get(p) ?? 0),
    };
  }

  function applyBarChart(
    chartData: BarChartData & { fromDate?: string; toDate?: string },
    title: string,
    grouping: BarChartGrouping
  ) {
    prevXGroupingSalesTitle.value = title;

    const rawLabels = chartData.labels;
    const rawData = chartData.datasets[0]?.data ?? [];

    let finalLabels: string[];
    let finalData: number[];

    if (chartData.fromDate && chartData.toDate) {
      const filled = fillMissingPeriods(rawLabels, rawData, grouping, chartData.fromDate, chartData.toDate);
      finalLabels = filled.labels;
      finalData = filled.data;
    } else {
      finalLabels = rawLabels;
      finalData = rawData;
    }

    prevXGroupingSales.value = {
      labels: formatBarChartLabels(grouping, finalLabels),
      datasets: [{
        label: "Sales",
        data: finalData,
      }],
    };
  }

  function applyPrev6MonthsSales(data: DashboardInterval[]) {
    prev6MonthsSales.value = {
      labels: data.map(d => d.grouping_name),
      datasets: [{
        label: "Sales",
        data: data.map(d => d.total),
      }],
    };
  }

  function applySalesByCategory(data: DashboardCategory[]) {
    salesByCategory.value = {
      labels: data.map(d => d.item_group),
      datasets: [{
        label: "Sales",
        data: data.map(d => d.total),
      }],
    };
  }

  function parseDashboardResults(results: any[]) {
    const monthData: DashboardInterval[] = [];

    results.forEach(row => {
      switch (row.metric_type) {
        case "sales_summary":
          applySalesSummary(row);
          break;
        case "purchase_summary":
          applyPurchaseSummary(row);
          break;
        case "expense_summary":
          applyExpenseSummary(row);
          break;
        case "sales_by_month":
          monthData.push(row);
          break;
        case "sales_by_category":
          applySalesByCategory(results.filter(r => r.metric_type === "sales_by_category"));
          break;
      }
    });

    if (monthData.length > 0) {
      applyPrev6MonthsSales(monthData);
    }

    results.forEach(row => {
      switch (row.metric_type) {
        case "gross_profit_summary":
          grossProfit.value = createCardData(row.cur_total, row.prev_total);
          break;
        case "gross_margin_summary":
          grossMargin.value = createCardData(row.cur_total, row.prev_total);
          break;
        case "net_profit_summary":
          netProfit.value = createCardData(row.cur_total, row.prev_total);
          break;
        case "net_margin_summary":
          netMargin.value = createCardData(row.cur_total, row.prev_total);
          break;
      }
    });
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
    prevXGroupingSalesTitle,
    prevXGroupingSales,
    parseDashboardResults,
    applySalesSummary,
    applyPurchaseSummary,
    applyExpenseSummary,
    applyBarChart,
    applyPrev6MonthsSales,
    applySalesByCategory,
  };
});
