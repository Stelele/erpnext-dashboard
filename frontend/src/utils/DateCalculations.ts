import type { GroupSummary } from "@/types/MonthSales";
import { getPeriodDateRangeFromCurrent, type Period } from "./PeriodUtilities";
import moment from "moment";

interface DateInterval {
  label: string;
  start: moment.Moment;
  end: moment.Moment;
}

export function getPeriodIntervals(period: Period): DateInterval[] | null {
  const groupedPeriods = [
    "This Week",
    "Last Week",
    "This Month",
    "Last Month",
    "This Quarter",
    "Last Quarter",
    "This Year",
    "Last Year",
  ];

  if (!groupedPeriods.includes(period)) {
    return null;
  }

  const dateRange = getPeriodDateRangeFromCurrent(period);
  let cur = moment(dateRange.start, "YYYY-MM-DD");
  const end = moment(dateRange.end, "YYYY-MM-DD");

  const intervals: DateInterval[] = [];
  let intervalType: moment.unitOfTime.StartOf;
  let labelFormat: (start: moment.Moment, end: moment.Moment) => string;

  if (period === "This Week" || period === "Last Week") {
    intervalType = "week";
    labelFormat = (start, end) =>
      `${start.format("DD MMM")} - ${end.format("DD MMM")}`;
  } else if (period === "This Month" || period === "Last Month") {
    intervalType = "month";
    labelFormat = (start) => start.format("MMM YY");
  } else {
    intervalType = "quarter";
    labelFormat = (start, end) =>
      `${start.format("MMM YY")} - ${end.format("MMM YY")}`;
  }

  // Generate the intervals
  while (cur.isBefore(end, "day")) {
    const intervalStart = moment(cur);
    const intervalEnd = moment(cur).endOf(intervalType);

    intervals.push({
      label: labelFormat(intervalStart, intervalEnd),
      start: intervalStart,
      end: intervalEnd,
    });

    cur = moment(intervalEnd).add(1, "days");
  }

  return intervals;
}

export function getTotalsForIntervals(
  intervals: DateInterval[],
  groupSales: GroupSummary[],
) {
  const labels: string[] = [];
  const data: number[] = [];

  intervals.forEach((interval) => {
    labels.push(interval.label);

    const sales = groupSales.filter((groupSale) =>
      moment(groupSale.grouping_name, "YYYY-MM-DD").isBetween(
        interval.start,
        interval.end,
        "day",
        "[]",
      ),
    );

    data.push(sales.reduce((acc, curr) => acc + curr.total, 0));
  });

  return { labels, data };
}

export function getPrevXYData(period: Period, groupSales: GroupSummary[]) {
  const intervals = getPeriodIntervals(period);

  if (intervals) {
    return getTotalsForIntervals(intervals, groupSales);
  }

  return {
    labels: groupSales.map((groupSale) =>
      moment(groupSale.grouping_name, "YYYY-MM-DD").format("DD MMM"),
    ),
    data: groupSales.map((groupSale) => groupSale.total),
  };
}
