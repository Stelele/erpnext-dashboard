<template>
    <UPageCard
        class="col-span-6 lg:col-span-6 h-96"
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
                class="w-full h-80 relative items-center justify-center chart-container"
            >
                <Line
                    v-if="props.data.datasets.length"
                    :data="chartData"
                    :options="chartOptions"
                />
            </div>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Line } from "vue-chartjs";
import type { ChartData, ChartOptions } from "chart.js";
import {
    Chart as ChartJS,
    Colors,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend,
} from "chart.js";
import { getChartJsColor } from "@/utils/ChartJsColors";
import { useColorMode } from "@vueuse/core";

ChartJS.register(
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend,
    Colors,
);

export interface LineChartProps {
    title: string;
    data: LineChartData;
    isLoading?: boolean;
}

export interface LineChartData {
    labels: string[];
    datasets: LineChartDataSet[];
}

export interface LineChartDataSet {
    label: string;
    data: number[];
}

const props = defineProps<LineChartProps>();
const colorMode = useColorMode();

const chartData = computed<ChartData<"line">>(() => {
    const data: ChartData<"line"> = {
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
            borderColor: getChartJsColor(i),
        });
    }

    return data;
});

const chartOptions = computed<ChartOptions<"line">>(() => ({
    responsive: true,
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
