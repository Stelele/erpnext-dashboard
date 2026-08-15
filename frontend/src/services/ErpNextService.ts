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
  PurchaseInvoiceResponse,
  AmendPurchasePayload,
  PackSizeEntry,
} from "@/types/Expenses";
import type { JournalEntry } from "@/types/JournalEntry";
import type {
  DailyStockValue,
  StockDetail,
  StockValueSummary,
} from "@/types/StockDetail";

type ErpNextResponse<T> = { message: T[] };
export type Grouping = "years" | "months" | "days";
export type AllAccountsResponse = {
  message: {
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
  last_selling_rate: number;
  description: string;
}

export interface WarehouseOption {
  name: string;
}

export interface PurchasePayload {
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number?: string;
  invoice_date: string;
  amended_from?: string;
}

export interface PurchaseResult {
  purchase_order: string;
  purchase_receipt: string;
  purchase_invoice: string;
  payment_entry: string;
}

export class ErpNextService {
  private instance: Axios;
  public packSizeMap?: PackSizeEntry[] | null;

  public constructor() {
    const authStore = useAuthStore();
    this.instance = axios.create();
    this.instance.interceptors.request.use((config) => {
      config.baseURL = authStore.url;
      config.headers.Authorization = `token ${authStore.token}`;
      return config;
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
        "/api/method/awesome_dashboard.api.dashboard.grouped_sales_summary",
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
      .then((resp) => resp?.data.message);
  }

  public getStockValueSummary(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get<ErpNextResponse<StockValueSummary>>(
        "/api/method/awesome_dashboard.api.stock.get_average_stock_value",
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
      .then((resp) => resp?.data.message);
  }

  public getDailyStockValueSummary(grouping: Grouping, diff: number) {
    const authStore = useAuthStore();

    return this.instance
      .get<ErpNextResponse<DailyStockValue>>(
        "/api/method/awesome_dashboard.api.stock.get_daily_stock_value",
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
      .then((resp) => resp?.data.message);
  }

  public getStockLevels() {
    const authStore = useAuthStore();

    const body: Record<string, unknown> = {
      company: authStore.company,
      warehouse: "Stores",
    };

    if (this.packSizeMap && this.packSizeMap.length > 0) {
      const apiMap: Record<string, { size: number; unit: string }> = {};
      for (const entry of this.packSizeMap) {
        apiMap[entry.itemName] = { size: entry.size, unit: entry.unit };
      }
      body.pack_size_map = apiMap;
    }

    return this.instance
      .post<ErpNextResponse<StockDetail>>("/api/method/awesome_dashboard.api.stock.get_stock_levels", body)
      .then((resp) => resp?.data.message);
  }

  public getPrevGroupedExpenses(grouping: Grouping, diff: number) {
    const authStore = useAuthStore();
    const groupingTemplate = this.getDateGrouping(grouping);

    return this.instance
      .get<ErpNextResponse<GroupSummary>>(
        "/api/method/awesome_dashboard.api.dashboard.grouped_expenses_summary",
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
      .then((resp) => resp?.data.message);
  }

  public getOrderBreakdown(period: Period) {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);
    return this.instance
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_order_breakdown", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);
  }

  public getExpenseBreakdown(period: Period) {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);
    return this.instance
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_expense_breakdown", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);
  }

  public async getAllAccounts() {
    const authStore = useAuthStore();

    const accounts = await this.instance
      .get<AllAccountsResponse>("/api/method/awesome_dashboard.api.finance.account_names", {
        params: {
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);

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
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_complete", {
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
      .then((resp) => resp?.data.message);
  }

  public getDashboardBarChart(fromDate: string, toDate: string, grouping: "day" | "week" | "month" | "quarter") {
    const authStore = useAuthStore();
    return this.instance
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_bar_chart", {
        params: {
          from_date: fromDate,
          to_date: toDate,
          grouping,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);
  }

  public getDashboardSalesAggregated(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_sales_aggregated", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);
  }

  public getDashboardPaymentEntries(period: Period = "Today") {
    const authStore = useAuthStore();
    const dateRange = getPeriodDateRange(period);

    return this.instance
      .get("/api/method/awesome_dashboard.api.dashboard.dashboard_payment_entries", {
        params: {
          from_date: dateRange.start,
          to_date: dateRange.end,
          company: authStore.company,
        },
      })
      .then((resp) => resp?.data.message);
  }

  public async submitExpenseJournalEntry(
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
      const doc = response.data.data;

      await this.instance.put(`/api/resource/Journal Entry/${doc.name}`, {
        docstatus: 1,
      });

      return doc;
    } catch {
      return undefined;
    }
  }

  public searchSuppliers(query: string) {
    const filters: unknown[] = [["disabled", "=", 0]];
    if (query) {
      filters.push(["supplier_name", "like", `%${query}%`]);
    }
    return this.instance
      .get<{ data: SupplierOption[] }>("/api/resource/Supplier", {
        params: {
          fields: JSON.stringify(["name", "supplier_name"]),
          filters: JSON.stringify(filters),
          limit_page_length: 200,
          order_by: "supplier_name",
          _: Date.now(),
        },
      })
      .then((resp) => resp?.data.data);
  }

  public createSupplier(supplierName: string) {
    return this.instance
      .post<{ data?: SupplierOption }>("/api/resource/Supplier", {
        supplier_name: supplierName,
      })
      .then((resp) => resp?.data.data);
  }

  public searchItems(query: string) {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<ItemOption>>("/api/method/awesome_dashboard.api.item.search_items", {
        params: { company: authStore.company, query },
      })
      .then((resp) => resp?.data.message);
  }

  public createItem(itemName: string, itemGroup: string, buyingPrice: number, sellingPrice: number) {
    const authStore = useAuthStore();
    return this.instance
      .post<{ message?: ItemOption }>("/api/method/awesome_dashboard.api.item.create_item", {
        company: authStore.company,
        item_name: itemName,
        item_group: itemGroup,
        buying_price: buyingPrice,
        selling_price: sellingPrice,
      })
      .then((resp) => resp?.data.message);
  }

  public getItemGroups() {
    return this.instance
      .get<{ data: { name: string }[] }>("/api/resource/Item Group", {
        params: { fields: '["name"]', limit_page_length: 500 },
      })
      .then((resp) => resp?.data.data || []);
  }

  public getWarehouses() {
    const authStore = useAuthStore();
    return this.instance
      .get<ErpNextResponse<WarehouseOption>>("/api/method/awesome_dashboard.api.lookup.search_warehouses", {
        params: { company: authStore.company },
      })
      .then((resp) => resp?.data.message);
  }

  public createFullPurchase(payload: PurchasePayload) {
    const authStore = useAuthStore();
    return this.instance
      .post<{ message?: PurchaseResult }>("/api/method/awesome_dashboard.api.purchase.create_full_purchase", {
        company: authStore.company,
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || "",
        invoice_date: payload.invoice_date,
      })
      .then((resp) => resp?.data.message)
      .catch(() => undefined);
  }

  public cancelFullPurchase(purchaseInvoice: string) {
    return this.instance
      .get<{ message?: { cancelled: string[]; message: string } }>("/api/method/awesome_dashboard.api.purchase.cancel_full_purchase", {
        params: { purchase_invoice: purchaseInvoice },
      })
      .then((resp) => resp?.data.message)
      .catch(() => undefined);
  }

  public async cancelExpenseJournalEntry(journalEntry: string) {
    try {
      await this.instance.put(`/api/resource/Journal Entry/${journalEntry}`, {
        docstatus: 2,
      });
      return true;
    } catch {
      return false;
    }
  }

  public async createStockReconciliation(payload: {
    warehouse: string;
    items: { item_code: string; qty: number; valuation_rate?: number }[];
    remarks?: string;
  }): Promise<boolean> {
    const authStore = useAuthStore();
    try {
      const now = moment();
      const body: Record<string, unknown> = {
        company: authStore.company,
        purpose: "Stock Reconciliation",
        set_posting_time: 1,
        posting_date: now.format("YYYY-MM-DD"),
        posting_time: now.format("HH:mm:ss"),
        items: payload.items.map((i) => ({
          item_code: i.item_code,
          warehouse: payload.warehouse,
          qty: i.qty,
          valuation_rate: i.valuation_rate || 1,
        })),
      };
      if (payload.remarks) {
        body.remarks = payload.remarks;
      }
      const createResp = await this.instance.post<{ data: { name: string } }>(
        "/api/resource/Stock Reconciliation",
        body,
      );
      await this.instance.put(
        `/api/resource/Stock Reconciliation/${createResp.data.data.name}`,
        { docstatus: 1 },
      );
      return true;
    } catch {
      return false;
    }
  }

  public async disableItem(
    itemCode: string,
    warehouse: string,
    remarks?: string,
  ): Promise<boolean> {
    try {
      const reconResult = await this.createStockReconciliation({
        warehouse,
        items: [{ item_code: itemCode, qty: 0 }],
        remarks: remarks || `Disabling item: ${itemCode}`,
      });
      if (!reconResult) return false;
      await this.instance.put(`/api/resource/Item/${encodeURIComponent(itemCode)}`, {
        disabled: 1,
      });
      return true;
    } catch {
      return false;
    }
  }

  public getJournalEntry(name: string) {
    return this.instance
      .get<{ data: JournalEntry }>(
        `/api/resource/Journal Entry/${encodeURIComponent(name)}`
      )
      .then((resp) => resp.data.data)
      .catch(() => undefined);
  }

  public async amendExpenseJournalEntry(
    originalId: string,
    expense: Expense,
    incomeAccount: AccountResponse,
    expenseAccount: AccountResponse,
  ) {
    const authStore = useAuthStore();
    try {
      const response = await this.instance.post<{ message?: { journal_entry: string } }>(
        "/api/method/awesome_dashboard.api.finance.amend_expense_journal_entry",
        {
          journal_entry: originalId,
          amount: expense.amount,
          description: expense.description,
          expense_account: expenseAccount.name,
          income_account: incomeAccount.name,
          posting_date: expense.date,
          company: authStore.company,
        }
      );
      return response.data.message?.journal_entry;
    } catch {
      return undefined;
    }
  }

  public amendFullPurchase(payload: AmendPurchasePayload) {
    return this.instance
      .post<{ message?: PurchaseResult }>("/api/method/awesome_dashboard.api.purchase.amend_full_purchase", {
        purchase_invoice: payload.originalId,
        company: payload.company,
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || "",
        invoice_date: payload.invoice_date,
      })
      .then((resp) => resp?.data.message)
      .catch(() => undefined);
  }

  public getPurchaseInvoice(name: string) {
    return this.instance
      .get<PurchaseInvoiceResponse>(
        `/api/resource/Purchase Invoice/${encodeURIComponent(name)}`
      )
      .then((resp) => resp.data)
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
