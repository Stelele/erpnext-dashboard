import type { Axios } from "axios";
import { useAuthStore } from "../stores/AuthStore";
import axios from "axios";
import { getPeriodDateRange, type Period } from "../utils/PeriodUtilities";
import moment from "moment";
import type { GroupSummary, ItemGroupSummary } from "../types/MonthSales";

type ErpNextResponse<T> = { data: T[] }
export type SalesGrouping = 'years' | 'months' | 'days'
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

    public async getExpensesSummary(period: Period = 'Today') {
        const authStore = useAuthStore()
        const dateRange = getPeriodDateRange(period)

        return this.instance.get<ErpNextResponse<GroupSummary>>('/api/v2/method/grouped_expenses_summary', {
            params: {
                from_date: dateRange.start,
                to_date: dateRange.end,
                company: authStore.company,
                time_grouping: this.getDateGrouping(this.getPeriodDateGrouping(period)),
            }
        }).then(resp => resp?.data.data)
    }

    public async getPurchaseGroupSummary(period: Period = 'Today') {
        const authStore = useAuthStore()
        const dateRange = getPeriodDateRange(period)

        return this.instance.get<ErpNextResponse<GroupSummary>>('/api/v2/method/grouped_purchase_order_summary', {
            params: {
                from_date: dateRange.start,
                to_date: dateRange.end,
                company: authStore.company,
                time_grouping: this.getDateGrouping(this.getPeriodDateGrouping(period)),
            }
        }).then(resp => resp?.data.data)
    }

    public async getSalesSummary(period: Period = 'Today') {
        const authStore = useAuthStore()
        const dateRange = getPeriodDateRange(period)

        return this.instance.get<ErpNextResponse<GroupSummary>>('/api/v2/method/grouped_sales_summary', {
            params: {
                from_date: dateRange.start,
                to_date: dateRange.end,
                company: authStore.company,
                time_grouping: this.getDateGrouping(this.getPeriodDateGrouping(period)),
            }
        }).then(resp => resp?.data.data)
    }

    public async getItemGroupSalesSummary(period: Period = 'Today') {
        const authStore = useAuthStore()
        const dateRange = getPeriodDateRange(period)

        return this.instance.get<ErpNextResponse<ItemGroupSummary>>('/api/v2/method/item_group_sales_summary', {
            params: {
                from_date: dateRange.start,
                to_date: dateRange.end,
                company: authStore.company,
                time_grouping: this.getDateGrouping(this.getPeriodDateGrouping(period)),
            }
        }).then(resp => resp?.data.data)
    }

    public async getPrevGroupedSales(grouping: SalesGrouping, diff: number) {
        const authStore = useAuthStore()
        const groupingTemplate = this.getDateGrouping(grouping)

        return this.instance.get<ErpNextResponse<GroupSummary>>('/api/v2/method/grouped_sales_summary', {
            params: {
                from_date: moment().subtract(diff, grouping).startOf(this.getGroupingStart(grouping)).format('YYYY-MM-DD'),
                to_date: moment().endOf('month').format('YYYY-MM-DD'),
                company: authStore.company,
                time_grouping: groupingTemplate,
            }
        }).then(resp => resp?.data.data)
    }

    private getDateGrouping(grouping: SalesGrouping) {
        switch (grouping) {
            case 'years':
                return '%%y'
            case 'months':
                return '%%Y-%%m'
            case 'days':
                return '%%Y-%%m-%%d'
        }
    }

    private getGroupingStart(grouping: SalesGrouping) {
        switch (grouping) {
            case 'years':
                return 'year'
            case 'months':
                return 'month'
            case 'days':
                return 'day'
        }
    }

    private getPeriodDateGrouping(period: Period) {
        switch (period) {
            case 'Today':
                return 'days'
            case 'Yesterday':
                return 'days'
            case 'This Week':
                return 'days'
            case 'Last Week':
                return 'days'
            case 'This Month':
                return 'months'
            case 'Last Month':
                return 'months'
            case 'This Quarter':
                return 'months'
            case 'Last Quarter':
                return 'months'
            case 'This Semester':
                return 'months'
            case 'Last Semester':
                return 'months'
            case 'This Year':
                return 'years'
            case 'Last Year':
                return 'years'
        }
    }
}