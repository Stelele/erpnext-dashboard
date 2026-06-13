import { ref } from 'vue'

const DEFAULT_COLORS = [
    '#00DC82',
    '#60A5FA',
    '#F87171',
    '#C084FC',
    '#FACC15',
    '#38BDF8',
    '#2DD4BF',
    '#94A3B8'
]

const chartJsColors = ref<string[]>([])

export function getChartJsColor(index: number): string {
    const palette = chartJsColors.value.length > 0 ? chartJsColors.value : DEFAULT_COLORS
    return palette[index % palette.length]!
}

export function setChartColors(colors: string[]): void {
    chartJsColors.value = colors
}
