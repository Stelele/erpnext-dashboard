<template>
    <UPageCard
        class="col-span-6 lg:col-span-4 h-96"
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
                <Bubble
                    v-if="chartData.datasets.length"
                    :data="chartData"
                    :options="chartOptions"
                />
            </div>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { Bubble } from "vue-chartjs";
import {
    Chart as ChartJS,
    Tooltip,
    Legend,
    PointElement,
    LinearScale,
    Title,
    type ChartOptions,
} from "chart.js";
import { getChartJsColor } from "@/utils/ChartJsColors";
import { useColorMode } from "@vueuse/core";
import { computed } from "vue";

// Register the required Chart.js components
ChartJS.register(LinearScale, PointElement, Tooltip, Legend, Title);

export interface CardBubbleChartProps {
    title: string;
    labels: { x: string; y: string };
    tooltipLabels: ToolTipLabel[];
    additionalData: BubbleDataPointAddition[][];
    datasets: BubbleDataset[];
}

export interface BubbleDataset {
    label: string;
    data: BubbleDataPoint[];
}

export interface BubbleDataPoint {
    x: number;
    y: number;
    r: number;
}
export interface BubbleDataPointAddition {
    [key: string]: string;
}
export interface ToolTipLabel {
    [key: string]: string;
}

const colorMode = useColorMode();
const props = defineProps<CardBubbleChartProps>();

const chartData = computed(() => {
    const maxRadius = 20;
    const minRadius = 5;

    JSON.stringify(props.tooltipLabels);
    JSON.stringify(props.additionalData);

    const datasets = props.datasets.map((dataset, idx) => {
        const maxR = Math.max(...dataset.data.map((dataPoint) => dataPoint.r));
        return {
            label: dataset.label,
            backgroundColor: getChartJsColor(idx),
            tooltipLabels: props.tooltipLabels[idx],
            additionalData: props.additionalData[idx],
            data: dataset.data.map((dataPoint) => ({
                x: dataPoint.x,
                y: dataPoint.y,
                r: Math.max(
                    minRadius,
                    Math.round((dataPoint.r / maxR) * maxRadius),
                ),
            })),
        };
    });

    return { datasets };
});

const chartOptions = computed<ChartOptions<"bubble">>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    scales: {
        x: {
            type: "linear",
            title: {
                display: true,
                color: colorMode.value === "dark" ? "#fff" : "#000",
                text: props.labels.x,
            },
            ticks: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
            grid: {
                display: true,
            },
        },
        y: {
            title: {
                display: true,
                color: colorMode.value === "dark" ? "#fff" : "#000",
                text: props.labels.y,
            },
            ticks: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
            grid: {
                display: true,
            },
        },
    },
    plugins: {
        legend: {
            labels: {
                color: colorMode.value === "dark" ? "#fff" : "#000",
            },
        },
        tooltip: {
            bodyFont: {
                family: "'Courier New', Courier, monospace",
                size: 13,
            },
            titleFont: {
                family: "'Courier New', Courier, monospace",
            },
            callbacks: {
                // Custom tooltip to show the real sales value instead of the scaled 'r' value
                label: function (context) {
                    const labels: ToolTipLabel = (context.dataset as any)
                        .tooltipLabels;
                    const raw = context.raw as BubbleDataPoint;

                    const keys = Object.keys(labels);
                    const maxLength = Math.max(
                        ...Object.values(labels).map((value) => value.length),
                    );

                    const formatLine = (label: string, value: any) => {
                        return `${label.padEnd(maxLength, " ")} : ${value}`;
                    };

                    const additionalData = (context.dataset as any)
                        .additionalData;
                    const additionalKeys = Object.keys(additionalData[0] ?? {});
                    const additionalLines = additionalKeys.map((key) =>
                        formatLine(
                            key,
                            (additionalData[context.dataIndex] ?? {})[key],
                        ),
                    );

                    return additionalLines.concat(
                        // @ts-ignore
                        keys.map((key) => formatLine(labels[key], raw[key])),
                    );
                },
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
