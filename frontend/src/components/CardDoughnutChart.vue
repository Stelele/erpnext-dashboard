<template>
    <UPageCard
        class="col-span-2 h-96"
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
                <Doughnut
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
import { Doughnut } from "vue-chartjs";
import type { ChartData, ChartOptions } from "chart.js";
import {
    Chart as ChartJS,
    ArcElement,
    Tooltip,
    Legend,
    Colors,
} from "chart.js";
import { getChartJsColor } from "@/utils/ChartJsColors";
import { useColorMode } from "@vueuse/core";

ChartJS.register(ArcElement, Tooltip, Legend, Colors);

export interface DoughnutChartProps {
    title: string;
    data: DoughnutChartData;
    isLoading?: boolean;
}

export interface DoughnutChartData {
    labels: string[];
    datasets: DoughnutChartDataSet[];
}

export interface DoughnutChartDataSet {
    label: string;
    data: number[];
}

const props = defineProps<DoughnutChartProps>();
const colorMode = useColorMode();

const chartData = computed<ChartData<"doughnut">>(() => {
    const data: ChartData<"doughnut"> = {
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
            backgroundColor: dataSet.data.map((_, idx) => getChartJsColor(idx)),
            borderColor: colorMode.value === "dark" ? "#0f172b" : undefined,
        });
    }

    return data;
});

const chartOptions = computed<ChartOptions<"doughnut">>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    layout: {
        autoPadding: true,
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
