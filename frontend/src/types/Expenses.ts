import type { PrimaryColor, NeutralColor } from '@/services/api/schema';

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

export interface CompanySettings {
  id: string;
  companyId: string;
  defaultIncomeAccountName: string;
  primaryColor?: PrimaryColor | null;
  neutralColor?: NeutralColor | null;
}

export interface Expense {
  date: string;
  expenseTypeId: string;
  amount: number;
  description: string;
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
