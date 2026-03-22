import { ExpenseAccountMapping, type Payment } from "../types/Expenses";
import type { GroupSummary } from "../types/MonthSales";
import type { DoughnutChartData, BarChartData } from "../types/ChartData";
import type { AccountMappings } from "@/services/ErpNextService";
import moment from "moment";

interface SerializedData {
  paymentEntries: Payment[];
  accountMappings: AccountMappings;
  prev6MonthsExpenses: GroupSummary[];
}

export interface ExpenseResults {
  expenseBreakdown: DoughnutChartData;
  orderBreakdown: DoughnutChartData;
  prev6MonthsExpenses: BarChartData;
}

function getExpenseType(
  account: string,
  accountMappings: AccountMappings,
): string {
  const expenseAccounts = Object.values(accountMappings.expenses);
  const retrievedAccount = expenseAccounts.find((ea) => ea.name === account);
  return (
    Object.entries(ExpenseAccountMapping).find(
      ([_, value]) => value === retrievedAccount?.account_name,
    )?.[0] ?? "Unknown"
  );
}

function computeExpenseBreakdown(data: SerializedData): DoughnutChartData {
  const expenses = data.paymentEntries.filter((pe) => pe.type === "Expense");
  const categories: Record<string, number> = {};
  for (const expense of expenses) {
    const expenseType = getExpenseType(
      expense.account ?? "",
      data.accountMappings,
    );
    categories[expenseType] = (categories[expenseType] ?? 0) + expense.amount;
  }

  const labels = Object.keys(categories).sort();
  const dataEntry = labels.map((label) => categories[label] ?? 0);

  return {
    labels,
    datasets: [
      {
        label: "Expense Value",
        data: dataEntry,
      },
    ],
  };
}

function computeOrderBreakdown(data: SerializedData): DoughnutChartData {
  const orders = data.paymentEntries.filter((pe) => pe.type === "Order");
  const categories: Record<string, number> = {};
  for (const order of orders) {
    categories[order.description] =
      (categories[order.description] ?? 0) + order.amount;
  }

  const labels = Object.keys(categories).sort();
  const dataEntry = labels.map((label) => categories[label] ?? 0);

  return {
    labels,
    datasets: [
      {
        label: "Order Value",
        data: dataEntry,
      },
    ],
  };
}

function computePrev6MonthsExpenses(data: SerializedData): BarChartData {
  const labels: string[] = [];
  const dataEntry: number[] = [];

  for (let i = 5; i >= 0; i--) {
    const month = moment().subtract(i, "months").format("YYYY-MM");
    const formattedMonth = moment(month, "YYYY-MM").format("MMM YYYY");
    labels.push(formattedMonth);
    const monthExpenses = data.prev6MonthsExpenses.find(
      (monthSale) => monthSale.grouping_name === month,
    );
    dataEntry.push(monthExpenses?.total ?? 0);
  }

  return {
    labels,
    datasets: [
      {
        label: "Expenses",
        data: dataEntry,
      },
    ],
  };
}

function computeExpenses(data: SerializedData): ExpenseResults {
  const expenseBreakdown = computeExpenseBreakdown(data);
  const orderBreakdown = computeOrderBreakdown(data);
  const prev6MonthsExpenses = computePrev6MonthsExpenses(data);

  return {
    expenseBreakdown,
    orderBreakdown,
    prev6MonthsExpenses,
  };
}

self.onmessage = (e: MessageEvent<SerializedData>) => {
  const results = computeExpenses(e.data);
  self.postMessage(results);
};
