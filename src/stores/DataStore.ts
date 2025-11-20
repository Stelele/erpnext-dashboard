import { defineStore } from "pinia";
import { ErpNextService } from "../services/ErpNextService";
import type { PosInvoice } from "../types/PosInvoice";
import { computed, ref } from "vue";
import { getPeriodDateRange, getPreviousPeriod, type Period } from "../utils/PeriodUtilities";
import type { GroupSummary, ItemGroupSummary } from "../types/MonthSales";
import moment from "moment";

export const useDataStore = defineStore('dataStore', () => {
    const salesSummary = ref<GroupSummary[]>([])
    const prevSalesSummary = ref<GroupSummary[] | undefined>(undefined)
    const prev6MonthsSales = ref<GroupSummary[]>([])
    const prevXGroupingSales = ref<GroupSummary[]>([])
    const itemGroupSalesSummary = ref<ItemGroupSummary[]>([])
    const purchaseGroupSummary = ref<GroupSummary[]>([])
    const prevPurchaseGroupSummary = ref<GroupSummary[] | undefined>(undefined)
    const expensesSummary = ref<GroupSummary[]>([])
    const prevExpensesSummary = ref<GroupSummary[] | undefined>(undefined)

    const currentPeriod = ref<Period>('Today')
    const lastRefresh = ref('')
    const dateRange = computed(() => getPeriodDateRange(currentPeriod.value))

    async function getData(period: Period) {
        const erpNextService = new ErpNextService()
        const prevPeriod = getPreviousPeriod(period)

        lastRefresh.value = moment().format('DD-MMM-YY HH:mm')
        const erpNextServicePromises = [
            erpNextService.getSalesSummary(period),
            prevPeriod ? erpNextService.getSalesSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
            erpNextService.getPrevGroupedSales('months', 6),
            erpNextService.getPrevGroupSalesFromCurrent(period),
            erpNextService.getItemGroupSalesSummary(period),
            erpNextService.getPurchaseGroupSummary(period),
            prevPeriod ? erpNextService.getPurchaseGroupSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
            erpNextService.getExpensesSummary(period),
            prevPeriod ? erpNextService.getExpensesSummary(prevPeriod) : new Promise<PosInvoice[] | undefined>(resolve => resolve(undefined)),
        ]

        const result = await Promise.all(erpNextServicePromises)
        salesSummary.value = (result[0] ?? []) as GroupSummary[]
        prevSalesSummary.value = result[1] as GroupSummary[] | undefined
        prev6MonthsSales.value = (result[2] ?? []) as GroupSummary[]
        prevXGroupingSales.value = (result[3] ?? []) as GroupSummary[]
        itemGroupSalesSummary.value = (result[4] ?? []) as ItemGroupSummary[]
        purchaseGroupSummary.value = (result[5] ?? []) as GroupSummary[]
        prevPurchaseGroupSummary.value = (result[6] ?? []) as GroupSummary[] | undefined
        expensesSummary.value = (result[7] ?? []) as GroupSummary[]
        prevExpensesSummary.value = result[8] as GroupSummary[] | undefined
    }

    return {
        currentPeriod,
        lastRefresh,
        dateRange,
        salesSummary,
        prevSalesSummary,
        prev6MonthsSales,
        prevXGroupingSales,
        itemGroupSalesSummary,
        purchaseGroupSummary,
        prevPurchaseGroupSummary,
        expensesSummary,
        prevExpensesSummary,
        getData,
    }
})