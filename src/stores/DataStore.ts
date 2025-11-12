import { defineStore } from "pinia";
import { ErpNextService } from "../services/ErpNextService";
import type { PosInvoice } from "../types/PosInvoice";
import { ref } from "vue";
import { getPreviousPeriod, type Period } from "../utils/PeriodUtilities";

export const useDataStore = defineStore('dataStore', () => {
    const posInvoices = ref<PosInvoice[]>([])
    const prevPosInvoices = ref<PosInvoice[] | undefined>(undefined)

    async function getData(period: Period) {
        const erpNextService = new ErpNextService()
        posInvoices.value = await erpNextService.getPosInvoices(period)

        const prevPeriod = getPreviousPeriod(period)
        if (prevPeriod) {
            prevPosInvoices.value = await erpNextService.getPosInvoices(prevPeriod)
        } else {
            prevPosInvoices.value = undefined
        }
    }

    return {
        posInvoices,
        prevPosInvoices,
        getData,
    }
})