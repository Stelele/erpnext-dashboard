<template>
    <UPageCard class="col-span-2 h-96">
        <CartTitle class="font-bold text-lg">{{ props.title }}</CartTitle>
        <div class="w-full h-full flex items-center justify-center">
            <Bar :data="chartData" :options="chartOptions" />
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import {
    Chart as ChartJS,
    Title,
    Tooltip,
    Legend,
    BarElement,
    CategoryScale,
    LinearScale,
    Colors,
} from 'chart.js'
import type { ChartData, ChartOptions } from 'chart.js'
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, Colors)

export interface CardBarChartProps {
    title: string
    indexAxis?: 'x' | 'y'
    data: BarChartData
}

export interface BarChartData {
    labels: string[]
    datasets: BarChartDataSet[]
}

export interface BarChartDataSet {
    label: string
    data: number[]
}

const props = withDefaults(defineProps<CardBarChartProps>(), {
    indexAxis: 'x'
})

const chartData = computed<ChartData<'bar'>>(() => {
    const data: ChartData<'bar'> = {
        labels: props.data.labels,
        datasets: []
    }

    const dataSets = props.data.datasets ?? []
    for (let i = 0; i < dataSets.length; i++) {
        const dataSet = dataSets[i]
        if (!dataSet) continue

        data.datasets.push({
            label: dataSet.label,
            data: dataSet.data,
        })
    }

    return data
})

const chartOptions = computed<ChartOptions<'bar'>>(() => ({
    responsive: true,
    indexAxis: props.indexAxis,
    maintainAspectRatio: false,
    scales: {
        x: {
            grid: {
                display: false
            }
        },
        y: {
            grid: {
                display: false
            }
        }
    }
}))

</script>