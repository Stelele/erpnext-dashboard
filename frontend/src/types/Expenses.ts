import type { PrimaryColor, NeutralColor, ThemeMode } from '@/services/api/schema';

export interface ExpenseType {
  id: string;
  name: string;
  description: string;
}

export interface CompanyExpenseMapping {
  id: string;
  expenseTypeId: string;
  expenseTypeName: string;
  erpnextAccountName: string;
}

export interface PackSizeEntry {
  itemName: string;
  size: number;
  unit: string;
}

export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
  primaryColor?: PrimaryColor | null;
  neutralColor?: NeutralColor | null;
  themeMode?: ThemeMode | null;
  packSizeMap?: PackSizeEntry[] | null;
}

export interface Expense {
  date: string;
  expenseTypeId: string;
  amount: number;
  description: string;
  amendEntryId?: string;
}

export interface AccountResponse {
  name: string;
  account_name: string;
}

export interface Payment {
  id: string;
  date: string;
  status: "Draft" | "Submitted" | "Cancelled";
  type: "Expense" | "Order";
  description: string;
  amount: number;
  account?: string;
}

export interface AccountMappings {
  expenses: Record<string, AccountResponse>;
  income: AccountResponse | null;
}

export interface PurchaseInvoiceItem {
  item_code: string;
  item_name: string;
  qty: number;
  rate: number;
  amount: number;
}

export interface PurchaseInvoiceResponse {
  data: {
    name: string;
    supplier: string;
    posting_date: string;
    grand_total: number;
    items: PurchaseInvoiceItem[];
  };
}

export interface AmendPurchasePayload {
  originalId: string;
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number?: string;
  invoice_date: string;
}
