<template>
    <UPageGrid>
        <NumberCard v-for="(item, idx) in items" :key="idx" :title="item.title" :value="item.value"
            :direction="item.direction" :percent-change="item.percentChange" />
        <CardBarChart title="Total Sales" :data="salesData" />
        <CardDoughnutChart title="Sales by Category" :data="salesByCategoryData" />
        <CardBarChart title="Top Performing Products" :data="topPerformingProducts" index-axis="y" />
    </UPageGrid>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import NumberCard from '../components/NumberCard.vue';
import CardBarChart, { type BarChartData } from '../components/CardBarChart.vue';
import CardDoughnutChart, { type DoughnutChartData } from '../components/CardDoughnutChart.vue';
import type { NumberCardProps } from '../components/NumberCard.vue';

const items = computed<NumberCardProps[]>(() => [
    { title: 'No. Sales', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Total Sales', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Avg. Sales', value: 1000, direction: 'flat', percentChange: 10 },
    { title: 'No. Purchases', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Total Purchases', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Avg. Purchases', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Gross Profit', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Gross Margin', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Total Expenses', value: 1000, direction: 'up', percentChange: 10 },
    { title: 'Avg. Expenses', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Net Profit', value: 1000, direction: 'down', percentChange: 10 },
    { title: 'Net Margin', value: 1000, direction: 'down', percentChange: 10 },
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

const salesByCategoryData = ref<DoughnutChartData>({
    labels: ['Jan', 'Feb', 'Mar', 'Apr'],
    datasets: [
        {
            label: 'Sales',
            data: [100, 200, 300, 400]
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