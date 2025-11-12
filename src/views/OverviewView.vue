<template>
    <UPageGrid>
        <NumberCard v-for="(item, idx) in items" :key="idx" :title="item.title" :value="item.value"
            :direction="item.direction" :percent-change="item.percentChange" />
        <CardBarChart title="Total Sales" :data="salesData" />
        <CardDoughnutChart title="Sales by Category" :data="overViewDataStore.salesByCategory" />
        <CardBarChart title="Top Performing Products" :data="topPerformingProducts" index-axis="y" />
    </UPageGrid>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import NumberCard from '../components/NumberCard.vue';
import CardBarChart, { type BarChartData } from '../components/CardBarChart.vue';
import CardDoughnutChart, { type DoughnutChartData } from '../components/CardDoughnutChart.vue';
import type { NumberCardProps } from '../components/NumberCard.vue';
import { useOverViewDataStore } from '../stores/OverViewDataStore';

const overViewDataStore = useOverViewDataStore()
const items = computed<NumberCardProps[]>(() => [
    { title: 'No. Sales', value: overViewDataStore.nrSales.value, direction: overViewDataStore.nrSales.direction, percentChange: overViewDataStore.nrSales.percentChange },
    { title: 'Total Sales ($)', value: overViewDataStore.totalSales.value, direction: overViewDataStore.totalSales.direction, percentChange: overViewDataStore.totalSales.percentChange },
    { title: 'Avg. Sales ($)', value: overViewDataStore.avgSales.value, direction: overViewDataStore.avgSales.direction, percentChange: overViewDataStore.avgSales.percentChange },
    { title: 'No. Orders', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Total Orders ($)', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Avg. Orders ($)', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Gross Profit ($)', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Gross Margin (%)', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Total Exp ($)', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Avg. Exp ($)', value: 1000, direction: 'flat', percentChange: 10 },
    { title: 'Net Profit ($)', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Net Margin (%)', value: 1000, direction: 'down', percentChange: 10 },
])

const salesData = ref<BarChartData>({
    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
    datasets: [
        {
            label: 'Sales',
            data: [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200]
        },
    ]
})

const topPerformingProducts = ref<BarChartData>({
    labels: ['beef', 'chicken', 'pork', 'lamb', 'fish', 'vegetables', 'fruits', 'dairy', 'eggs', 'oil'],
    datasets: [
        {
            label: 'Sales',
            data: [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000].reverse()
        },
    ]
})
</script>