import { defineStore } from "pinia";
import { useDataStore } from "./DataStore";
import { computed } from "vue";
import type { CardData } from "../types/CardData";
import { calculatePercentChange } from "../utils/ChangeCalculations";
import type { DoughnutChartData } from "../components/CardDoughnutChart.vue";

export const useOverViewDataStore = defineStore('overViewDataStore', () => {
    const dataStore = useDataStore()

    const nrSales = computed<CardData>(() => {
        const curLength = dataStore.posInvoices.length
        const prevLength = dataStore.prevPosInvoices?.length
        const change = calculatePercentChange(curLength, prevLength)

        return {
            value: curLength,
            prevValue: prevLength,
            direction: change?.direction,
            percentChange: change?.percentChange ?? 0,
        }
    })

    const totalSales = computed<CardData>(() => {
        const curTotal = dataStore.posInvoices.reduce((acc, curr) => acc + curr.total, 0)
        const prevTotal = dataStore.prevPosInvoices?.reduce((acc, curr) => acc + curr.total, 0)
        const change = calculatePercentChange(curTotal, prevTotal)

        return {
            value: curTotal,
            prevValue: prevTotal,
            direction: change?.direction,
            percentChange: change?.percentChange ?? 0,
        }
    })

    const avgSales = computed<CardData>(() => {
        const curAvg = totalSales.value.value / nrSales.value.value
        const prevAvg = totalSales.value.prevValue && nrSales.value.prevValue
            ? totalSales.value.prevValue / nrSales.value.prevValue
            : undefined
        const change = calculatePercentChange(curAvg, prevAvg)

        return {
            value: curAvg,
            direction: change?.direction,
            percentChange: change?.percentChange ?? 0,
        }
    })

    const salesByCategory = computed<DoughnutChartData>(() => {
        const categories: Record<string, number> = {}
        for (const posInvoice of dataStore.posInvoices) {
            for (const item of posInvoice.items) {
                categories[item.item_group] = (categories[item.item_group] ?? 0) + item.amount
            }
        }

        const labels = Object.keys(categories).sort()
        const data = labels.map(label => categories[label] ?? 0)

        return {
            labels,
            datasets: [
                {
                    label: 'Sales',
                    data,
                },
            ]
        }
    })

    return {
        nrSales,
        totalSales,
        avgSales,
        salesByCategory,
    }
})