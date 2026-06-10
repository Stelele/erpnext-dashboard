import type { Axios } from "axios";
import { useAuthStore } from "@/stores/AuthStore";
import axios from "axios";
import {
  getPeriodDateRange,
  type Period,
} from "@/utils/PeriodUtilities";
import moment from "moment";
import type { GroupSummary } from "@/types/MonthSales";
import type {
  Expense,
  CompanyExpenseMapping,
  AccountMappings,
  AccountResponse,
} from "@/types/Expenses";
import type { JournalEntry } from "@/types/JournalEntry";
import type {
  DailyStockValue,
  StockDetail,
  StockValueSummary,
} from "@/types/StockDetail";

type ErpNextResponse<T> = { data: T[] };
export type Grouping = "years" | "months" | "days";
export type AllAccountsResponse = {
  data: {
    expense: AccountResponse[];
    income: AccountResponse[];
  };
};

export interface SupplierOption {
  name: string;
  supplier_name: string;
}

export interface ItemOption {
  item_code: string;
  item_name: string;
  last_purchase_rate: number;
}

export interface WarehouseOption {
  name: string;
}

export interface PurchasePayload {
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number }[];
  invoice_number?: string;
  invoice_date: string;
}

export interface PurchaseResult {
  purchase_order: string;
  purchase_receipt: string;
  purchase_invoice: string;
  payment_entry: string;
}

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

  public static async getCompanyLogo(
    companyName: string,
    siteUrl: string,
    siteToken: string,
  ): Promise<string | undefined> {
    try {
      const response = await axios.get(
        `${siteUrl}/api/resource/Company/${encodeURIComponent(companyName)}`,
        {
          headers: {
            Authorization: `token ${siteToken}`,
          },
        },
      );
      return response.data.data?.company_logo;
    } catch {
      return undefined;
    }
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
          warehouse: "Stores",
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

  public async getAllAccounts() {
    const authStore = useAuthStore();

    const accounts = await this.instance
      .get<AllAccountsResponse>("/api/v2/method/account_names", {
        params: {
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.data);

    return accounts;
  }

  public async getAccountMappings(
    expenseMappings: CompanyExpenseMapping[],
    incomeAccountName: string,
  ): Promise<AccountMappings> {
    const accounts = await this.getAllAccounts();

    const expenses: Record<string, AccountResponse> = {};
    for (const mapping of expenseMappings) {
      const account = accounts.expense.find(
        (a) => a.account_name === mapping.erpnextAccountName,
      );
      if (account) {
        expenses[mapping.expenseTypeId] = account;
      }
    }

    const income = accounts.income.find(
      (a) => a.account_name === incomeAccountName,
    ) ?? null;

    return { expenses, income };
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
          warehouse: "Stores",
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
    const body = {
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

    try {
      const response = await this.instance.post<{ data: JournalEntry }>(
        "/api/resource/Journal Entry",
        body,
      );
      return response.data.data;
    } catch {
      return undefined;
    }
  }

  public searchSuppliers(query: string) {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<SupplierOption>>("/api/v2/method/search_suppliers", {
        params: { company: authStore.company, query },
      })
      .then((resp) => resp?.data.data);
  }

  public searchItems(query: string) {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<ItemOption>>("/api/v2/method/search_items", {
        params: { company: authStore.company, query },
      })
      .then((resp) => resp?.data.data);
  }

  public getWarehouses() {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<WarehouseOption>>("/api/v2/method/search_warehouses", {
        params: { company: authStore.company },
      })
      .then((resp) => resp?.data.data);
  }

  public createFullPurchase(payload: PurchasePayload) {
    const authStore = useAuthStore();
    return this.instance
      .post<{ data?: PurchaseResult }>("/api/v2/method/create_full_purchase", {
        company: authStore.company,
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || "",
        invoice_date: payload.invoice_date,
      })
      .then((resp) => resp?.data.data)
      .catch(() => undefined);
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
