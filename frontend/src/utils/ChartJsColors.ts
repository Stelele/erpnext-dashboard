import { useColorMode } from '@vueuse/core'

const chartJsDefautlColors = [
    '#00DC82', // nuxt green
    '#2563EB', // blue-600
    '#F43F5E', // rose-500
    '#A855F7', // purple-500
    '#EAB308', // yellow-500
    '#0EA5E9', // sky-500
    '#14B8A6', // teal-500
    '#64748B'  // slate-500
]

const chartJsDarkColors = [
    '#00DC82', // primary green
    '#60A5FA', // blue-400
    '#F87171', // red-400
    '#C084FC', // purple-400
    '#FACC15', // yellow-400
    '#38BDF8', // sky-400
    '#2DD4BF', // teal-400
    '#94A3B8'  // slate-400
]

export function getChartJsColor(index: number) {
    const colorMode = useColorMode()
    if (colorMode.value === 'dark') {
        return chartJsDarkColors[index % chartJsDarkColors.length]
    }

    return chartJsDefautlColors[index % chartJsDefautlColors.length]
}