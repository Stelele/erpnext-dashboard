<template>
    <DashboardLayout>
        <NumberCard
            v-for="(item, idx) in items"
            :key="idx"
            :title="item.title"
            :value="item.value"
            :direction="item.direction"
            :percent-change="item.percentChange"
        />
        <CardBarChart
            :title="overViewDataStore.prevXGroupingSales.title"
            :data="overViewDataStore.prevXGroupingSales.data"
        />
        <CardDoughnutChart
            title="Sales by Category"
            :data="overViewDataStore.salesByCategory"
        />
        <CardBarChart
            title="Sales from last 6 months"
            :data="overViewDataStore.prev6MonthsSales"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import { computed } from "vue";
import NumberCard from "@/components/NumberCard.vue";
import CardBarChart from "@/components/CardBarChart.vue";
import CardDoughnutChart from "@/components/CardDoughnutChart.vue";
import type { NumberCardProps } from "@/components/NumberCard.vue";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import { useOverViewDataStore } from "@/stores/OverViewDataStore";
import { useDataStore } from "@/stores/DataStore";

const overViewDataStore = useOverViewDataStore();
const dataStore = useDataStore();
const items = computed<NumberCardProps[]>(() => [
    {
        title: "No. Sales",
        value: overViewDataStore.nrSales.value,
        direction: overViewDataStore.nrSales.direction,
        percentChange: overViewDataStore.nrSales.percentChange,
    },
    {
        title: "Total Sales ($)",
        value: overViewDataStore.totalSales.value,
        direction: overViewDataStore.totalSales.direction,
        percentChange: overViewDataStore.totalSales.percentChange,
    },
    {
        title: "Avg. Sales ($)",
        value: overViewDataStore.avgSales.value,
        direction: overViewDataStore.avgSales.direction,
        percentChange: overViewDataStore.avgSales.percentChange,
    },
    {
        title: "No. Orders",
        value: overViewDataStore.nrPurchases.value,
        direction: overViewDataStore.nrPurchases.direction,
        percentChange: overViewDataStore.nrPurchases.percentChange,
    },
    {
        title: "Total Orders ($)",
        value: overViewDataStore.totalPurchases.value,
        direction: overViewDataStore.totalPurchases.direction,
        percentChange: overViewDataStore.totalPurchases.percentChange,
    },
    {
        title: "Avg. Orders ($)",
        value: overViewDataStore.avgPurchases.value,
        direction: overViewDataStore.avgPurchases.direction,
        percentChange: overViewDataStore.avgPurchases.percentChange,
    },
    {
        title: "Gross Profit ($)",
        value: overViewDataStore.grossProfit.value,
        direction: overViewDataStore.grossProfit.direction,
        percentChange: overViewDataStore.grossProfit.percentChange,
    },
    {
        title: "Gross Margin (%)",
        value: overViewDataStore.grossMargin.value,
        direction: overViewDataStore.grossMargin.direction,
        percentChange: overViewDataStore.grossMargin.percentChange,
    },
    {
        title: "Total Exp ($)",
        value: overViewDataStore.totalExpenses.value,
        direction: overViewDataStore.totalExpenses.direction,
        percentChange: overViewDataStore.totalExpenses.percentChange,
    },
    {
        title: "Avg. Exp ($)",
        value: overViewDataStore.avgExpenses.value,
        direction: overViewDataStore.avgExpenses.direction,
        percentChange: overViewDataStore.avgExpenses.percentChange,
    },
    {
        title: "Net Profit ($)",
        value: overViewDataStore.netProfit.value,
        direction: overViewDataStore.netProfit.direction,
        percentChange: overViewDataStore.netProfit.percentChange,
    },
    {
        title: "Net Margin (%)",
        value: overViewDataStore.netMargin.value,
        direction: overViewDataStore.netMargin.direction,
        percentChange: overViewDataStore.netMargin.percentChange,
    },
]);
</script>
