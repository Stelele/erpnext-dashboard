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
        <CardBubbleChart
            :isLoading="dataStore.loading"
            :title="stockDataStore.salesVsStock.title"
            :labels="stockDataStore.salesVsStock.labels"
            :additionalData="stockDataStore.salesVsStock.additionalData"
            :datasets="stockDataStore.salesVsStock.datasets"
            :tooltip-labels="stockDataStore.salesVsStock.tooltipLabels"
        />
        <CardDoughnutChart
            title="Stock Value By Item Group"
            :data="stockDataStore.stockByItemGroup"
        />
        <CardLineChart
            title="Daily Stock Value"
            :data="stockDataStore.dailyStockValues"
        />
        <StockTable
            :data="stockDataStore.stockTableData"
            :loading="dataStore.loading"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import type { NumberCardProps } from "@/components/NumberCard.vue";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import { useDataStore } from "@/stores/DataStore";
import { useStockDataStore } from "@/stores/StockDataStore";
import { computed } from "vue";

const dataStore = useDataStore();
const stockDataStore = useStockDataStore();
const items = computed<NumberCardProps[]>(() => [
    {
        title: "Total Stock Value ($)",
        value: stockDataStore.totalStockValue,
    },
    {
        title: "Total Selling Value ($)",
        value: stockDataStore.totalSellingValue,
    },
    {
        title: "Avg. Markup Percentage (%)",
        value: stockDataStore.averageMarkupPercentage,
    },
]);
</script>
