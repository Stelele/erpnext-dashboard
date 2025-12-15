import moment from "moment"

export type Period = 'Today' | 'Yesterday' | 'This Week' | 'Last Week' | 'This Month' | 'Last Month' | 'This Quarter' | 'Last Quarter' | 'This Semester' | 'Last Semester' | 'This Year' | 'Last Year'
export type PeriodDateRange = {
    start: string
    end: string
}
export function getPeriodDateRange(period: Period): PeriodDateRange {
    const curSemesterStartQuarter = moment().quarter() % 2 === 0 ? moment().quarter() - 1 : moment().quarter()

    switch (period) {
        case 'Today':
            return {
                start: moment().startOf('day').format('YYYY-MM-DD'),
                end: moment().endOf('day').format('YYYY-MM-DD')
            }
        case 'Yesterday':
            return {
                start: moment().subtract(1, 'day').startOf('day').format('YYYY-MM-DD'),
                end: moment().subtract(1, 'day').endOf('day').format('YYYY-MM-DD')
            }
        case 'This Week':
            return {
                start: moment().startOf('week').format('YYYY-MM-DD'),
                end: moment().endOf('week').format('YYYY-MM-DD')
            }
        case 'Last Week':
            return {
                start: moment().subtract(1, 'week').startOf('week').format('YYYY-MM-DD'),
                end: moment().subtract(1, 'week').endOf('week').format('YYYY-MM-DD')
            }
        case 'This Month':
            return {
                start: moment().startOf('month').format('YYYY-MM-DD'),
                end: moment().endOf('month').format('YYYY-MM-DD')
            }
        case 'Last Month':
            return {
                start: moment().subtract(1, 'month').startOf('month').format('YYYY-MM-DD'),
                end: moment().subtract(1, 'month').endOf('month').format('YYYY-MM-DD')
            }
        case 'This Quarter':
            return {
                start: moment().startOf('quarter').format('YYYY-MM-DD'),
                end: moment().endOf('quarter').format('YYYY-MM-DD')
            }
        case 'Last Quarter':
            return {
                start: moment().subtract(1, 'quarter').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().subtract(1, 'quarter').endOf('quarter').format('YYYY-MM-DD')
            }
        case 'This Semester':
            return {
                start: moment().quarter(curSemesterStartQuarter).startOf('quarter').format('YYYY-MM-DD'),
                end: moment().quarter(curSemesterStartQuarter + 1).endOf('quarter').format('YYYY-MM-DD')
            }
        case 'Last Semester':
            return {
                start: moment().quarter(curSemesterStartQuarter).subtract(2, 'quarters').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().quarter(curSemesterStartQuarter).subtract(1, 'quarters').endOf('quarter').format('YYYY-MM-DD')
            }
        case 'This Year':
            return {
                start: moment().startOf('year').format('YYYY-MM-DD'),
                end: moment().endOf('year').format('YYYY-MM-DD')
            }
        case 'Last Year':
            return {
                start: moment().subtract(1, 'year').startOf('year').format('YYYY-MM-DD'),
                end: moment().subtract(1, 'year').endOf('year').format('YYYY-MM-DD')
            }
    }
}

export function getPreviousPeriod(period: Period) {
    switch (period) {
        case 'Today':
            return 'Yesterday'
        case 'This Week':
            return 'Last Week'
        case 'This Month':
            return 'Last Month'
        case 'This Quarter':
            return 'Last Quarter'
        case 'This Semester':
            return 'Last Semester'
        case 'This Year':
            return 'Last Year'
        default:
            return undefined
    }
}

export function getPeriodDateRangeFromCurrent(period: Period): PeriodDateRange {
    switch (period) {
        case 'Today':
            return {
                start: moment().subtract(7, 'days').format('YYYY-MM-DD'),
                end: moment().format('YYYY-MM-DD')
            }
        case 'Yesterday':
            return {
                start: moment().subtract(7, 'days').format('YYYY-MM-DD'),
                end: moment().format('YYYY-MM-DD')
            }
        case 'This Week':
            return {
                start: moment().subtract(4, 'weeks').startOf('week').format('YYYY-MM-DD'),
                end: moment().endOf('week').format('YYYY-MM-DD')
            }
        case 'Last Week':
            return {
                start: moment().subtract(4, 'weeks').startOf('week').format('YYYY-MM-DD'),
                end: moment().endOf('week').format('YYYY-MM-DD')
            }
        case 'This Month':
            return {
                start: moment().subtract(3, 'months').startOf('month').format('YYYY-MM-DD'),
                end: moment().endOf('month').format('YYYY-MM-DD')
            }
        case 'Last Month':
            return {
                start: moment().subtract(3, 'months').startOf('month').format('YYYY-MM-DD'),
                end: moment().endOf('month').format('YYYY-MM-DD')
            }
        case 'This Quarter':
            return {
                start: moment().subtract(1, 'quarters').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().endOf('quarter').format('YYYY-MM-DD')
            }
        case 'Last Quarter':
            return {
                start: moment().subtract(1, 'quarters').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().endOf('quarter').format('YYYY-MM-DD')
            }
        case 'This Year':
            return {
                start: moment().subtract(4, 'quarters').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().endOf('quarter').format('YYYY-MM-DD')
            }
        case 'Last Year':
            return {
                start: moment().subtract(4, 'quarters').startOf('quarter').format('YYYY-MM-DD'),
                end: moment().endOf('quarter').format('YYYY-MM-DD')
            }
    }

    return {
        start: moment().startOf('month').format('YYYY-MM-DD'),
        end: moment().endOf('month').format('YYYY-MM-DD')
    }
}