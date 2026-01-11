<template>
    <UPageCard
        class="col-span-6 lg:col-span-2"
        :title="props.title"
        :ui="{
            container: 'gap-y-1.5',
            wrapper: 'items-start',
            leading:
                'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
            title: 'font-bold text-xs uppercase',
        }"
    >
        <div class="w-full h-full max-h-80">
            <div
                class="w-full h-full flex items-center justify-center chart-container"
            >
                <Bar
                    v-if="chartData"
                    :data="chartData"
                    :options="chartOptions"
                />
            </div>
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
} from "chart.js";
import type { ChartData, ChartOptions } from "chart.js";
import { computed } from "vue";
import { Bar } from "vue-chartjs";
import { getChartJsColor } from "@/utils/ChartJsColors";
import { useColorMode } from "@vueuse/core";

ChartJS.register(
    CategoryScale,
    LinearScale,
    BarElement,
    Title,
    Tooltip,
    Legend,
    Colors,
);

export interface CardBarChartProps {
    title: string;
    indexAxis?: "x" | "y";
    data: BarChartData;
}

export interface BarChartData {
    labels: string[];
    datasets: BarChartDataSet[];
}

export interface BarChartDataSet {
    label: string;
    data: number[];
}

const props = withDefaults(defineProps<CardBarChartProps>(), {
    indexAxis: "x",
});
const colorMode = useColorMode();

const chartData = computed<ChartData<"bar">>(() => {
    const data: ChartData<"bar"> = {
        labels: props.data.labels,
        datasets: [],
    };

    const dataSets = props.data.datasets ?? [];
    for (let i = 0; i < dataSets.length; i++) {
        const dataSet = dataSets[i];
        if (!dataSet) continue;

        data.datasets.push({
            label: dataSet.label,
            data: dataSet.data,
            backgroundColor: getChartJsColor(i),
            borderColor: colorMode.value === "dark" ? "#0f172b" : undefined,
        });
    }

    return data;
});

const chartOptions = computed<ChartOptions<"bar">>(() => ({
    responsive: true,
    indexAxis: props.indexAxis,
    maintainAspectRatio: false,
    scales: {
        x: {
            ticks: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
            grid: {
                display: false,
            },
        },
        y: {
            ticks: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
            grid: {
                display: false,
            },
        },
    },
    plugins: {
        legend: {
            labels: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
        },
    },
}));
</script>

<style scoped>
.chart-container {
    width: 100%;
    /* Or a fixed width if desired */
    height: 100%;
    /* Or a percentage height if its parent has a defined height */
}

.chart-container canvas {
    width: 100% !important;
    height: 100% !important;
}
</style>
