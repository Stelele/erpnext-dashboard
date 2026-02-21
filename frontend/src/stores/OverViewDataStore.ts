import { defineStore } from "pinia";
import { useDataStore } from "./DataStore";
import { ref } from "vue";
import type { CardData } from "../types/CardData";
import { calculatePercentChange } from "../utils/ChangeCalculations";
import type { DoughnutChartData } from "../components/CardDoughnutChart.vue";
import type { BarChartData } from "../components/CardBarChart.vue";
import moment from "moment";
import { type Period } from "../utils/PeriodUtilities";
import { getPrevXYData } from "@/utils/DateCalculations";

export const useOverViewDataStore = defineStore("overViewDataStore", () => {
  const dataStore = useDataStore();

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

  function update() {
    nrSales.value = getNrSales();
    totalSales.value = getTotalSales();
    avgSales.value = getAvgSales();
    nrPurchases.value = getNrPurchases();
    totalPurchases.value = getTotalPurchases();
    avgPurchases.value = getAvgPurchases();
    grossProfit.value = getGrossProfit();
    grossMargin.value = getGrossMargin();
    nrExpenses.value = getNrExpenses();
    totalExpenses.value = getTotalExpenses();
    avgExpenses.value = getAvgExpenses();
    netProfit.value = getNetProfit();
    netMargin.value = getNetMargin();
    salesByCategory.value = getSalesByCategory();
    prevXGroupingSales.value = getPrevXGroupingSales();
    prev6MonthsSales.value = getPrev6MonthsSales();

    function getNrSales() {
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
    }

    function getTotalSales() {
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
    }

    function getAvgSales() {
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
    }

    function getNrPurchases() {
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
    }

    function getTotalPurchases() {
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
    }

    function getAvgPurchases() {
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
    }

    function getGrossProfit() {
      const curGrossProfit =
        totalSales.value.value - totalPurchases.value.value;
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
    }

    function getGrossMargin() {
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
    }

    function getNrExpenses() {
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
    }

    function getTotalExpenses() {
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
    }

    function getAvgExpenses() {
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
    }

    function getNetProfit() {
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
    }

    function getNetMargin() {
      const curNetMargin =
        (netProfit.value.value / totalSales.value.value) * 100;
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
    }

    function getSalesByCategory() {
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
    }

    function getPrevXGroupingSales() {
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
    }

    function getPrev6MonthsSales() {
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
    }
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
    update,
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
