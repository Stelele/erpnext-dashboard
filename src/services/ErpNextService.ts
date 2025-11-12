import type { Axios } from "axios";
import { useAuthStore } from "../stores/AuthStore";
import axios from "axios";
import type { PosInvoice } from "../types/PosInvoice";
import { getPeriodDateRange, type Period } from "../utils/PeriodUtilities";

type ErpNextResponse<T> = { data: T[] }
export class ErpNextService {
    private instance: Axios

    public constructor() {
        const authStore = useAuthStore()
        this.instance = axios.create({
            baseURL: authStore.url,
            headers: {
                Authorization: `token ${authStore.token}`,
                'Content-Type': 'application/json',
            },
        })
    }

    public async getPosInvoices(period: Period = 'Today') {
        const authStore = useAuthStore()

        const dateRange = getPeriodDateRange(period)
        const posOpeningFields = ["name", "status", "posting_date"];
        const posOpeningFilters = [
            ["POS Invoice", "docstatus", "=", "1"],
            ["POS Invoice", "company", "=", authStore.company],
            ["POS Invoice", "status", "in", ["Paid", "Consolidated"]],
            ["POS Invoice", "posting_date", "between", [dateRange.start, dateRange.end]],
        ]

        const initialResponse = (await this.instance.get<ErpNextResponse<Pick<PosInvoice, 'name' | 'status' | 'posting_date'>>>(
            `/api/v2/document/POS Invoice`, {
            params: {
                fields: JSON.stringify(posOpeningFields),
                filters: JSON.stringify(posOpeningFilters),
                limit: 0,
            }
        })).data?.data ?? []

        const posInvoicePromises: Promise<PosInvoice | undefined>[] = []
        initialResponse.forEach((item) => {
            posInvoicePromises.push(
                this.instance
                    .get<{ data: PosInvoice }>(`/api/v2/document/POS Invoice/${item.name}`)
                    .then(resp => resp?.data?.data)
                    .catch(_ => undefined))
        })

        return (await Promise.all(posInvoicePromises)).filter(item => item !== undefined)
    }
}