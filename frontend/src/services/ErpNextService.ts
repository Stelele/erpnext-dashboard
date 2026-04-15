import type { Axios } from "axios";
import { useAuthStore } from "@/stores/AuthStore";
import axios from "axios";
import {
  getPeriodDateRange,
  type Period,
} from "@/utils/PeriodUtilities";
import { ExpenseAccountMapping, IncomeAccountMapping } from "@/types/Expenses";
import moment from "moment";
import type { GroupSummary } from "@/types/MonthSales";
import type {
  Expense,
  ExpenseType,
  IncomeType,
  Payment,
} from "@/types/Expenses";
import type { JournalEntry } from "@/types/JournalEntry";
import type { SalesDetail } from "@/types/SalesDetail";
import type {
  DailyStockValue,
  StockDetail,
  StockValueSummary,
} from "@/types/StockDetail";

type ErpNextResponse<T> = { data: T[] };
export type Grouping = "years" | "months" | "days";
export type AccountResponse = { account_name: string; name: string };
export type AllAccountsResponse = {
  data: {
    expense: AccountResponse[];
    income: AccountResponse[];
  };
};
export type AccountMappings = {
  expenses: Record<ExpenseType, AccountResponse>;
  incomes: Record<IncomeType, AccountResponse>;
};

export class ErpNextService {
  private instance: Axios;

  public constructor() {
    const authStore = useAuthStore();
    this.instance = axios.create({
      baseURL: authStore.url,
      headers: {
        Authorization: `token ${authStore.token}`,
        "Content-Type": "application/json",
      },
    });
  }

  public getSalesSummary(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get<ErpNextResponse<GroupSummary>>(
        "/api/v2/method/grouped_sales_summary",
        {
          params: {
            from_date: dateRange.start,
            to_date: dateRange.end,
            company: authStore.company,
            time_grouping: this.getDateGrouping(
              this.getPeriodDateGrouping(period),
            ),
          },
        },
      )
      .then((resp) => resp?.data.data);
  }

  public getStockValueSummary(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get<ErpNextResponse<StockValueSummary>>(
        "/api/v2/method/get_average_stock_value",
        {
          params: {
            from_date: dateRange.start,
            to_date: dateRange.end,
            company: authStore.company,
            time_grouping: this.getDateGrouping(
              this.getPeriodDateGrouping(period),
            ),
          },
        },
      )
      .then((resp) => resp?.data.data);
  }

  public getDailyStockValueSummary(grouping: Grouping, diff: number) {
    const authStore = useAuthStore();

    return this.instance
      .get<ErpNextResponse<DailyStockValue>>(
        "/api/v2/method/get_daily_stock_value",
        {
          params: {
            from_date: moment()
              .subtract(diff, grouping)
              .startOf(this.getGroupingStart(grouping))
              .format("YYYY-MM-DD"),
            to_date: moment().endOf("day").format("YYYY-MM-DD"),
            company: authStore.company,
          },
        },
      )
      .then((resp) => resp?.data.data);
  }

  public getStockLevels() {
    const authStore = useAuthStore();

    return this.instance
      .get<ErpNextResponse<StockDetail>>("/api/v2/method/get_stock_levels", {
        params: {
          company: authStore.company,
          warehouse: "Stores - NEs",
        },
      })
      .then((resp) => resp?.data.data);
  }

  public getPrevGroupedExpenses(grouping: Grouping, diff: number) {
    const authStore = useAuthStore();
    const groupingTemplate = this.getDateGrouping(grouping);

    return this.instance
      .get<ErpNextResponse<GroupSummary>>(
        "/api/v2/method/grouped_expenses_summary",
        {
          params: {
            from_date: moment()
              .subtract(diff, grouping)
              .startOf(this.getGroupingStart(grouping))
              .format("YYYY-MM-DD"),
            to_date: moment().endOf("month").format("YYYY-MM-DD"),
            company: authStore.company,
            time_grouping: groupingTemplate,
          },
        },
      )
      .then((resp) => resp?.data.data);
  }

  public getOrderBreakdown(period: Period) {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);
    return this.instance
      .get("/api/v2/method/dashboard_order_breakdown", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);
  }

  public getExpenseBreakdown(period: Period) {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);
    return this.instance
      .get("/api/v2/method/dashboard_expense_breakdown", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);
  }

  public async getAccountMappings() {
    const authStore = useAuthStore();

    const accounts = await this.instance
      .get<AllAccountsResponse>("/api/v2/method/account_names", {
        params: {
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);

    const expenses: Record<ExpenseType, AccountResponse> = {} as Record<
      ExpenseType,
      AccountResponse
    >;
    Object.keys(ExpenseAccountMapping).forEach((expenseType) => {
      const account = accounts.expense.find(
        (account) =>
          ExpenseAccountMapping[expenseType as ExpenseType] ===
          account.account_name,
      );
      expenses[expenseType as ExpenseType] = account!;
    });

    const incomes: Record<IncomeType, AccountResponse> = {} as Record<
      IncomeType,
      AccountResponse
    >;
    Object.keys(IncomeAccountMapping).forEach((incomeType) => {
      const account = accounts.income.find(
        (account) =>
          IncomeAccountMapping[incomeType as IncomeType] ===
          account.account_name,
      );
      incomes[incomeType as IncomeType] = account!;
    });

    return { expenses, incomes } as AccountMappings;
  }

  public getDashboardComplete(period: Period, prevPeriod: Period | undefined) {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);
    const prevDateRange = prevPeriod ? getPeriodDateRange(prevPeriod) : getPeriodDateRange(period);

    return this.instance
      .get("/api/v2/method/dashboard_complete", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          prev_from_date: prevDateRange.start,
          prev_to_date: prevDateRange.end,
          company: authStore.company,
          warehouse: "Stores - NEs",
          time_grouping: this.getDateGrouping(
            this.getPeriodDateGrouping(period),
          ),
        },
      })
      .then((resp) => resp?.data.data);
  }

  public getDashboardBarChart(fromDate: string, toDate: string, grouping: "day" | "week" | "month" | "quarter") {
    const authStore = useAuthStore();
    return this.instance
      .get("/api/v2/method/dashboard_bar_chart", {
        params: {
          from_date: fromDate,
          to_date: toDate,
          grouping,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);
  }

  public getDashboardSalesAggregated(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get("/api/v2/method/dashboard_sales_aggregated", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);
  }

  public getDashboardPaymentEntries(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get("/api/v2/method/dashboard_payment_entries", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);
  }

  public async addDraftExpenseJournalEntry(
    expense: Expense,
    incomeAccount: AccountResponse,
    expenseAccount: AccountResponse,
  ) {
    const authStore = useAuthStore();
    const entry = {
      voucher_type: "Journal Entry",
      company: authStore.company,
      posting_date: expense.date,
      user_remark: expense.description,
      accounts: [
        {
          account: expenseAccount.name,
          debit_in_account_currency: expense.amount,
        },
        {
          account: incomeAccount.name,
          credit_in_account_currency: expense.amount,
        },
      ],
    };

    return this.instance
      .post("/api/resource/Journal Entry", entry)
      .then((resp) => resp?.data.data as JournalEntry)
      .catch((error) => {
        console.error(error);
        return undefined;
      });
  }

  private getDateGrouping(grouping: Grouping) {
    switch (grouping) {
      case "years":
        return "%%y";
      case "months":
        return "%%Y-%%m";
      case "days":
        return "%%Y-%%m-%%d";
    }
  }

  private getGroupingStart(grouping: Grouping) {
    switch (grouping) {
      case "years":
        return "year";
      case "months":
        return "month";
      case "days":
        return "day";
    }
  }

  private getPeriodDateGrouping(period: Period) {
    switch (period) {
      case "Today":
        return "days";
      case "Yesterday":
        return "days";
      case "This Week":
        return "days";
      case "Last Week":
        return "days";
      case "This Month":
        return "months";
      case "Last Month":
        return "months";
      case "This Quarter":
        return "months";
      case "Last Quarter":
        return "months";
      case "This Semester":
        return "months";
      case "Last Semester":
        return "months";
      case "This Year":
        return "years";
      case "Last Year":
        return "years";
      case "Last 12 Months":
        return "months";
    }
  }
}
