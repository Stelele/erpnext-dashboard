<template>
    <UPageCard class="col-span-2 h-96">
        <CartTitle class="font-bold text-lg">{{ props.title }}</CartTitle>
        <div class="w-full h-full flex items-center justify-center">
            <Doughnut v-if="props.data.datasets.length" :data="chartData" :options="chartOptions" />
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Doughnut } from 'vue-chartjs'
import type { ChartData, ChartOptions } from 'chart.js'
import { Chart as ChartJS, ArcElement, Tooltip, Legend, Colors } from 'chart.js'
import { getChartJsColor } from '../utils/ChartJsColors'
import { useColorMode } from '@vueuse/core'

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

const props = defineProps<DoughnutChartProps>()
const colorMode = useColorMode()

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
            backgroundColor: dataSet.data.map((_, idx) => getChartJsColor(idx)),
            borderColor: colorMode.value === 'dark' ? '#0f172b' : undefined,
        })
    }

    return data
})

const chartOptions = computed<ChartOptions<'doughnut'>>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            labels: {
                color: colorMode.value === 'dark' ? '#fff' : '#000'
            }
        }
    }
}))

</script>