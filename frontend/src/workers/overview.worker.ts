import type { GroupSummary, ItemGroupSummary } from "../types/MonthSales";
import type { Period } from "../utils/PeriodUtilities";
import type {
  CardData,
  DoughnutChartData,
  BarChartData,
} from "../types/ChartData";
import type { ChangeDirection } from "@/components/NumberCard.vue";
import {
  getPeriodIntervals,
  getTotalsForIntervals,
} from "@/utils/DateCalculations";
import moment from "moment";

interface SerializedData {
  salesSummary: GroupSummary[];
  prevSalesSummary: GroupSummary[] | undefined;
  prevXGroupingSales: GroupSummary[];
  prev6MonthsSales: GroupSummary[];
  purchaseGroupSummary: GroupSummary[];
  prevPurchaseGroupSummary: GroupSummary[] | undefined;
  expensesSummary: GroupSummary[];
  prevExpensesSummary: GroupSummary[] | undefined;
  itemGroupSalesSummary: ItemGroupSummary[];
  currentPeriod: Period;
}

export interface OverviewResults {
  nrSales: CardData;
  totalSales: CardData;
  avgSales: CardData;
  nrPurchases: CardData;
  totalPurchases: CardData;
  avgPurchases: CardData;
  grossProfit: CardData;
  grossMargin: CardData;
  nrExpenses: CardData;
  totalExpenses: CardData;
  avgExpenses: CardData;
  netProfit: CardData;
  netMargin: CardData;
  salesByCategory: DoughnutChartData;
  prevXGroupingSales: { data: BarChartData; title: string };
  prev6MonthsSales: BarChartData;
}

function calculatePercentChange(curValue: number, prevValue?: number) {
  if (!prevValue) {
    return undefined;
  }

  const percentChange = Math.round(((curValue - prevValue) / prevValue) * 100);
  const direction: ChangeDirection =
    percentChange > 0 ? "up" : percentChange < 0 ? "down" : "flat";

  return {
    percentChange: Math.abs(percentChange),
    direction,
  };
}

function getTitle(period: Period) {
  switch (period) {
    case "Today":
      return "Sales from last 7 days";
    case "Yesterday":
      return "Sales from last 7 days";
    case "This Week":
      return "Sales from last 4 weeks";
    case "Last Week":
      return "Sales from last 4 weeks";
    case "This Month":
      return `Sales from last 3 months`;
    case "Last Month":
      return `Sales from last 3 months`;
    case "This Quarter":
      return `Sales from last quarter`;
    case "Last Quarter":
      return `Sales from last quarter`;
    case "This Year":
      return `Sales from last 4 quarters`;
    case "Last Year":
      return `Sales from last 4 quarters`;
  }
}

function computeOverview(data: SerializedData): OverviewResults {
  const nrSales = computeNrSales(data);
  const totalSales = computeTotalSales(data);
  const avgSales = computeAvgSales(totalSales, nrSales);
  const nrPurchases = computeNrPurchases(data);
  const totalPurchases = computeTotalPurchases(data);
  const avgPurchases = computeAvgPurchases(totalPurchases, nrPurchases);
  const grossProfit = computeGrossProfit(totalSales, totalPurchases);
  const grossMargin = computeGrossMargin(grossProfit, totalSales);
  const nrExpenses = computeNrExpenses(data);
  const totalExpenses = computeTotalExpenses(data);
  const avgExpenses = computeAvgExpenses(totalExpenses, nrExpenses);
  const netProfit = computeNetProfit(grossProfit, totalExpenses);
  const netMargin = computeNetMargin(netProfit, totalSales);
  const salesByCategory = computeSalesByCategory(data);
  const prevXGroupingSales = computePrevXGroupingSales(data);
  const prev6MonthsSales = computePrev6MonthsSales(data);

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
    prevXGroupingSales,
    prev6MonthsSales,
  };
}

function computeNrSales(data: SerializedData): CardData {
  const curLength = data.salesSummary.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const prevLength = data.prevSalesSummary?.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const change = calculatePercentChange(curLength, prevLength);

  return {
    value: curLength,
    prevValue: prevLength,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeTotalSales(data: SerializedData): CardData {
  const curTotal = data.salesSummary.reduce((acc, curr) => acc + curr.total, 0);
  const prevTotal = data.prevSalesSummary?.reduce(
    (acc, curr) => acc + curr.total,
    0,
  );
  const change = calculatePercentChange(curTotal, prevTotal);

  return {
    value: curTotal,
    prevValue: prevTotal,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeAvgSales(totalSales: CardData, nrSales: CardData): CardData {
  const curAvg = totalSales.value / nrSales.value;
  const prevAvg =
    totalSales.prevValue && nrSales.prevValue
      ? totalSales.prevValue / nrSales.prevValue
      : undefined;
  const change = calculatePercentChange(curAvg, prevAvg);

  return {
    value: curAvg,
    prevValue: prevAvg,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeNrPurchases(data: SerializedData): CardData {
  const curLength = data.purchaseGroupSummary.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const prevLength = data.prevPurchaseGroupSummary?.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const change = calculatePercentChange(curLength, prevLength);

  return {
    value: curLength,
    prevValue: prevLength,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeTotalPurchases(data: SerializedData): CardData {
  const curTotal = data.purchaseGroupSummary.reduce(
    (acc, curr) => acc + curr.total,
    0,
  );
  const prevTotal = data.prevPurchaseGroupSummary?.reduce(
    (acc, curr) => acc + curr.total,
    0,
  );
  const change = calculatePercentChange(curTotal, prevTotal);

  return {
    value: curTotal,
    prevValue: prevTotal,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeAvgPurchases(
  totalPurchases: CardData,
  nrPurchases: CardData,
): CardData {
  const curAvg = totalPurchases.value / nrPurchases.value;
  const prevAvg =
    totalPurchases.prevValue && nrPurchases.prevValue
      ? totalPurchases.prevValue / nrPurchases.prevValue
      : undefined;
  const change = calculatePercentChange(curAvg, prevAvg);

  return {
    value: curAvg,
    prevValue: prevAvg,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeGrossProfit(
  totalSales: CardData,
  totalPurchases: CardData,
): CardData {
  const curGrossProfit = totalSales.value - totalPurchases.value;
  const prevGrossProfit =
    totalSales.prevValue && totalPurchases.prevValue
      ? totalSales.prevValue - totalPurchases.prevValue
      : undefined;
  const change = calculatePercentChange(curGrossProfit, prevGrossProfit);

  return {
    value: curGrossProfit,
    prevValue: prevGrossProfit,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeGrossMargin(
  grossProfit: CardData,
  totalSales: CardData,
): CardData {
  const curGrossMargin = (grossProfit.value / totalSales.value) * 100;
  const prevGrossMargin =
    grossProfit.prevValue && totalSales.prevValue
      ? (grossProfit.prevValue / totalSales.prevValue) * 100
      : undefined;
  const change = calculatePercentChange(curGrossMargin, prevGrossMargin);

  return {
    value: curGrossMargin,
    prevValue: prevGrossMargin,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeNrExpenses(data: SerializedData): CardData {
  const curLength = data.expensesSummary.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const prevLength = data.prevExpensesSummary?.reduce(
    (acc, curr) => acc + curr.count,
    0,
  );
  const change = calculatePercentChange(curLength, prevLength);

  return {
    value: curLength,
    prevValue: prevLength,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeTotalExpenses(data: SerializedData): CardData {
  const curTotal = data.expensesSummary.reduce(
    (acc, curr) => acc + curr.total,
    0,
  );
  const prevTotal = data.prevExpensesSummary?.reduce(
    (acc, curr) => acc + curr.total,
    0,
  );
  const change = calculatePercentChange(curTotal, prevTotal);

  return {
    value: curTotal,
    prevValue: prevTotal,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeAvgExpenses(
  totalExpenses: CardData,
  nrExpenses: CardData,
): CardData {
  const curAvg = totalExpenses.value / nrExpenses.value;
  const prevAvg =
    totalExpenses.prevValue && nrExpenses.prevValue
      ? totalExpenses.prevValue / nrExpenses.prevValue
      : undefined;
  const change = calculatePercentChange(curAvg, prevAvg);

  return {
    value: curAvg,
    prevValue: prevAvg,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeNetProfit(
  grossProfit: CardData,
  totalExpenses: CardData,
): CardData {
  const curNetProfit = grossProfit.value - totalExpenses.value;
  const prevNetProfit =
    grossProfit.prevValue && totalExpenses.prevValue
      ? grossProfit.prevValue - totalExpenses.prevValue
      : undefined;
  const change = calculatePercentChange(curNetProfit, prevNetProfit);

  return {
    value: curNetProfit,
    prevValue: prevNetProfit,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeNetMargin(netProfit: CardData, totalSales: CardData): CardData {
  const curNetMargin = (netProfit.value / totalSales.value) * 100;
  const prevNetMargin =
    netProfit.prevValue && totalSales.prevValue
      ? (netProfit.prevValue / totalSales.prevValue) * 100
      : undefined;
  const change = calculatePercentChange(curNetMargin, prevNetMargin);

  return {
    value: curNetMargin,
    prevValue: prevNetMargin,
    direction: change?.direction,
    percentChange: change?.percentChange ?? 0,
  };
}

function computeSalesByCategory(data: SerializedData): DoughnutChartData {
  const categories: Record<string, number> = {};
  for (const summary of data.itemGroupSalesSummary) {
    categories[summary.item_group] =
      (categories[summary.item_group] ?? 0) + summary.total;
  }

  const labels = Object.keys(categories).sort();
  const dataEntry = labels.map((label) => categories[label] ?? 0);

  return {
    labels,
    datasets: [
      {
        label: "Sales",
        data: dataEntry,
      },
    ],
  };
}

function getPrevXYData(
  period: Period,
  groupSales: GroupSummary[],
): { labels: string[]; data: number[] } {
  const intervals = getPeriodIntervals(period);

  if (intervals) {
    return getTotalsForIntervals(intervals, groupSales);
  }

  return {
    labels: groupSales.map((groupSale) =>
      moment(groupSale.grouping_name, "YYYY-MM-DD").format("DD MMM"),
    ),
    data: groupSales.map((groupSale) => groupSale.total),
  };
}

function computePrevXGroupingSales(data: SerializedData): {
  data: BarChartData;
  title: string;
} {
  const { labels, data: dataEntry } = getPrevXYData(
    data.currentPeriod,
    data.prevXGroupingSales,
  );

  return {
    title: getTitle(data.currentPeriod) as string,
    data: {
      labels,
      datasets: [
        {
          label: "Sales",
          data: dataEntry,
        },
      ],
    },
  };
}

function computePrev6MonthsSales(data: SerializedData): BarChartData {
  const labels: string[] = [];
  const dataEntry: number[] = [];

  for (let i = 5; i >= 0; i--) {
    const month = moment().subtract(i, "months").format("YYYY-MM");
    const formattedMonth = moment(month, "YYYY-MM").format("MMM YYYY");
    labels.push(formattedMonth);
    const monthSales = data.prev6MonthsSales.find(
      (monthSale) => monthSale.grouping_name === month,
    );
    dataEntry.push(monthSales?.total ?? 0);
  }

  return {
    labels,
    datasets: [
      {
        label: "Sales",
        data: dataEntry,
      },
    ],
  };
}

self.onmessage = (e: MessageEvent<SerializedData>) => {
  const results = computeOverview(e.data);
  self.postMessage(results);
};
