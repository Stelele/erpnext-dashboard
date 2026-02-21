import { defineStore } from "pinia";
import { useDataStore } from "./DataStore";
import { computed } from "vue";
import type { CardData } from "../types/CardData";
import { calculatePercentChange } from "../utils/ChangeCalculations";
import type { DoughnutChartData } from "../components/CardDoughnutChart.vue";
import type { BarChartData } from "../components/CardBarChart.vue";
import moment from "moment";
import {
  getPeriodDateRangeFromCurrent,
  type Period,
} from "../utils/PeriodUtilities";
import type { GroupSummary } from "../types/MonthSales";
import { getPrevXYData } from "@/utils/DateCalculations";

export const useOverViewDataStore = defineStore("overViewDataStore", () => {
  const dataStore = useDataStore();

  const nrSales = computed<CardData>(() => {
    const curLength = dataStore.salesSummary.reduce(
      (acc, curr) => acc + curr.count,
      0,
    );
    const prevLength = dataStore.prevSalesSummary?.reduce(
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
  });

  const totalSales = computed<CardData>(() => {
    const curTotal = dataStore.salesSummary.reduce(
      (acc, curr) => acc + curr.total,
      0,
    );
    const prevTotal = dataStore.prevSalesSummary?.reduce(
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
  });

  const avgSales = computed<CardData>(() => {
    const curAvg = totalSales.value.value / nrSales.value.value;
    const prevAvg =
      totalSales.value.prevValue && nrSales.value.prevValue
        ? totalSales.value.prevValue / nrSales.value.prevValue
        : undefined;
    const change = calculatePercentChange(curAvg, prevAvg);

    return {
      value: curAvg,
      prevValue: prevAvg,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const nrPurchases = computed<CardData>(() => {
    const curLength = dataStore.purchaseGroupSummary.reduce(
      (acc, curr) => acc + curr.count,
      0,
    );
    const prevLength = dataStore.prevPurchaseGroupSummary?.reduce(
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
  });

  const totalPurchases = computed<CardData>(() => {
    const curTotal = dataStore.purchaseGroupSummary.reduce(
      (acc, curr) => acc + curr.total,
      0,
    );
    const prevTotal = dataStore.prevPurchaseGroupSummary?.reduce(
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
  });

  const avgPurchases = computed<CardData>(() => {
    const curAvg = totalPurchases.value.value / nrPurchases.value.value;
    const prevAvg =
      totalPurchases.value.prevValue && nrPurchases.value.prevValue
        ? totalPurchases.value.prevValue / nrPurchases.value.prevValue
        : undefined;
    const change = calculatePercentChange(curAvg, prevAvg);

    return {
      value: curAvg,
      prevValue: prevAvg,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const grossProfit = computed<CardData>(() => {
    const curGrossProfit = totalSales.value.value - totalPurchases.value.value;
    const prevGrossProfit =
      totalSales.value.prevValue && totalPurchases.value.prevValue
        ? totalSales.value.prevValue - totalPurchases.value.prevValue
        : undefined;
    const change = calculatePercentChange(curGrossProfit, prevGrossProfit);

    return {
      value: curGrossProfit,
      prevValue: prevGrossProfit,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const grossMargin = computed<CardData>(() => {
    const curGrossMargin =
      (grossProfit.value.value / totalSales.value.value) * 100;
    const prevGrossMargin =
      grossProfit.value.prevValue && totalSales.value.prevValue
        ? (grossProfit.value.prevValue / totalSales.value.prevValue) * 100
        : undefined;
    const change = calculatePercentChange(curGrossMargin, prevGrossMargin);

    return {
      value: curGrossMargin,
      prevValue: prevGrossMargin,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const nrExpenses = computed<CardData>(() => {
    const curLength = dataStore.expensesSummary.reduce(
      (acc, curr) => acc + curr.count,
      0,
    );
    const prevLength = dataStore.prevExpensesSummary?.reduce(
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
  });

  const totalExpenses = computed<CardData>(() => {
    const curTotal = dataStore.expensesSummary.reduce(
      (acc, curr) => acc + curr.total,
      0,
    );
    const prevTotal = dataStore.prevExpensesSummary?.reduce(
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
  });

  const avgExpenses = computed<CardData>(() => {
    const curAvg = totalExpenses.value.value / nrExpenses.value.value;
    const prevAvg =
      totalExpenses.value.prevValue && nrExpenses.value.prevValue
        ? totalExpenses.value.prevValue / nrExpenses.value.prevValue
        : undefined;
    const change = calculatePercentChange(curAvg, prevAvg);

    return {
      value: curAvg,
      prevValue: prevAvg,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const netProfit = computed<CardData>(() => {
    const curNetProfit = grossProfit.value.value - totalExpenses.value.value;
    const prevNetProfit =
      grossProfit.value.prevValue && totalExpenses.value.prevValue
        ? grossProfit.value.prevValue - totalExpenses.value.prevValue
        : undefined;
    const change = calculatePercentChange(curNetProfit, prevNetProfit);

    return {
      value: curNetProfit,
      prevValue: prevNetProfit,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const netMargin = computed<CardData>(() => {
    const curNetMargin = (netProfit.value.value / totalSales.value.value) * 100;
    const prevNetMargin =
      netProfit.value.prevValue && totalSales.value.prevValue
        ? (netProfit.value.prevValue / totalSales.value.prevValue) * 100
        : undefined;
    const change = calculatePercentChange(curNetMargin, prevNetMargin);

    return {
      value: curNetMargin,
      prevValue: prevNetMargin,
      direction: change?.direction,
      percentChange: change?.percentChange ?? 0,
    };
  });

  const salesByCategory = computed<DoughnutChartData>(() => {
    const categories: Record<string, number> = {};
    for (const summary of dataStore.itemGroupSalesSummary) {
      categories[summary.item_group] =
        (categories[summary.item_group] ?? 0) + summary.total;
    }

    const labels = Object.keys(categories).sort();
    const data = labels.map((label) => categories[label] ?? 0);

    return {
      labels,
      datasets: [
        {
          label: "Sales",
          data,
        },
      ],
    };
  });

  const prevXGroupingSales = computed<{ data: BarChartData; title: string }>(
    () => {
      const { labels, data } = getPrevXYData(
        dataStore.currentPeriod,
        dataStore.prevXGroupingSales,
      );

      return {
        title: getTitle(dataStore.currentPeriod) as string,
        data: {
          labels,
          datasets: [
            {
              label: "Sales",
              data,
            },
          ],
        },
      };
    },
  );

  const prev6MonthsSales = computed<BarChartData>(() => {
    const labels: string[] = [];
    const data: number[] = [];

    for (let i = 5; i >= 0; i--) {
      const month = moment().subtract(i, "months").format("YYYY-MM");
      const formattedMonth = moment(month, "YYYY-MM").format("MMM YYYY");
      labels.push(formattedMonth);
      const monthSales = dataStore.prev6MonthsSales.find(
        (monthSale) => monthSale.grouping_name === month,
      );
      data.push(monthSales?.total ?? 0);
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
  });

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
  };
});

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
