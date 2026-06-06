import moment from "moment";
import type { Period } from "@/utils/PeriodUtilities";

export interface BarChartConfig {
  fromDate: string;
  toDate: string;
  grouping: "day" | "week" | "month" | "quarter";
}

export function getBarChartConfig(period: Period): BarChartConfig {
  const today = moment();
  const monthStart = today.clone().startOf("month");

  switch (period) {
    case "Today":
    case "Yesterday":
      return {
        fromDate: monthStart.clone().subtract(14, "days").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "day" as const,
      };
    case "This Week":
    case "Last Week":
      return {
        fromDate: monthStart.clone().subtract(5, "weeks").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "week" as const,
      };
    case "This Month":
    case "Last Month":
      return {
        fromDate: monthStart.clone().subtract(3, "months").startOf("month").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "month" as const,
      };
    case "This Quarter":
    case "Last Quarter":
      return {
        fromDate: monthStart.clone().subtract(4, "quarters").startOf("quarter").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "quarter" as const,
      };
    case "This Semester":
    case "Last Semester":
      return {
        fromDate: monthStart.clone().subtract(6, "quarters").startOf("quarter").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "quarter" as const,
      };
    case "This Year":
    case "Last Year":
      return {
        fromDate: monthStart.clone().subtract(3, "years").startOf("years").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "quarter" as const,
      };
    default:
      return {
        fromDate: monthStart.clone().subtract(14, "days").format("YYYY-MM-DD"),
        toDate: today.format("YYYY-MM-DD"),
        grouping: "day" as const,
      };
  }
}

export function getBarChartTitle(period: Period): string {
  switch (period) {
    case "Today":
    case "Yesterday":
      return "Sales from last 14 days";
    case "This Week":
    case "Last Week":
      return "Sales from last 4 weeks";
    case "This Month":
    case "Last Month":
      return "Sales from last 3 months";
    case "This Quarter":
    case "Last Quarter":
      return "Sales from last 4 quarters";
    case "This Semester":
    case "Last Semester":
      return "Sales from last 6 quarters";
    case "This Year":
    case "Last Year":
      return "Sales from last 3 years";
    default:
      return "Sales from last 14 days";
  }
}
