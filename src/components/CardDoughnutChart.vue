<template>
    <UPageCard class="col-span-2 h-96">
        <CartTitle class="font-bold text-lg">{{ props.title }}</CartTitle>
        <div class="w-full h-full flex items-center justify-center">
            <Doughnut :data="chartData" :options="chartOptions" />
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import type { ChartData, ChartOptions } from 'chart.js'
import { Chart as ChartJS, ArcElement, Tooltip, Legend, Colors } from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend, Colors)

export interface DoughnutChartProps {
    title: string
    data: DoughnutChartData
}

export interface DoughnutChartData {
    labels: string[]
    datasets: DoughnutChartDataSet[]
}

export interface DoughnutChartDataSet {
    label: string
    data: number[]
}

const chartData = computed<ChartData<'doughnut'>>(() => {
    const data: ChartData<'doughnut'> = {
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

const chartOptions = computed<ChartOptions<'doughnut'>>(() => ({
    responsive: true,
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

const props = defineProps<DoughnutChartProps>()
</script>