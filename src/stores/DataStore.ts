import { defineStore } from "pinia";
import { ErpNextService } from "../services/ErpNextService";
import type { PosInvoice } from "../types/PosInvoice";
import { computed, ref } from "vue";
import { getPeriodDateRange, getPreviousPeriod, type Period } from "../utils/PeriodUtilities";
import type { GroupSummary, ItemGroupSummary } from "../types/MonthSales";

export const useDataStore = defineStore('dataStore', () => {
    const salesSummary = ref<GroupSummary[]>([])
    const prevSalesSummary = ref<GroupSummary[] | undefined>(undefined)
    const prev12MonthsSales = ref<GroupSummary[]>([])
    const prev14DaysSales = ref<GroupSummary[]>([])
    const itemGroupSalesSummary = ref<ItemGroupSummary[]>([])
    const purchaseGroupSummary = ref<GroupSummary[]>([])
    const prevPurchaseGroupSummary = ref<GroupSummary[] | undefined>(undefined)
    const expensesSummary = ref<GroupSummary[]>([])
    const prevExpensesSummary = ref<GroupSummary[] | undefined>(undefined)

    const currentPeriod = ref<Period>('This Month')
    const dateRange = computed(() => getPeriodDateRange(currentPeriod.value))

    async function getData(period: Period) {
        const erpNextService = new ErpNextService()
        const prevPeriod = getPreviousPeriod(period)

        const erpNextServicePromises: Promise<any>[] = [
            erpNextService.getSalesSummary(period),
            prevPeriod ? erpNextService.getSalesSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
            erpNextService.getPrevGroupedSales('months', 12),
            erpNextService.getPrevGroupedSales('days', 14),
            erpNextService.getItemGroupSalesSummary(period),
            erpNextService.getPurchaseGroupSummary(period),
            prevPeriod ? erpNextService.getPurchaseGroupSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
            erpNextService.getExpensesSummary(period),
            prevPeriod ? erpNextService.getExpensesSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
        ]

        const result = await Promise.all(erpNextServicePromises)
        salesSummary.value = result[0] ?? []
        prevSalesSummary.value = result[1]
        prev12MonthsSales.value = result[2] ?? []
        prev14DaysSales.value = result[3] ?? []
        itemGroupSalesSummary.value = result[4] ?? []
        purchaseGroupSummary.value = result[5] ?? []
        prevPurchaseGroupSummary.value = result[6]
        expensesSummary.value = result[7] ?? []
        prevExpensesSummary.value = result[8]
    }

    return {
        currentPeriod,
        dateRange,
        salesSummary,
        prevSalesSummary,
        prev12MonthsSales,
        prev14DaysSales,
        itemGroupSalesSummary,
        purchaseGroupSummary,
        prevPurchaseGroupSummary,
        expensesSummary,
        prevExpensesSummary,
        getData,
    }
})