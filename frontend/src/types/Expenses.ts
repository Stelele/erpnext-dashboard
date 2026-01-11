export type ExpenseType =
  | "Sekuru"
  | "Canteen"
  | "Spoiled Meat"
  | "Utilities"
  | "Consumables"
  | "Neg Variance"
  | "Other";
export type IncomeType = "Pos Variance" | "Cash";

export type ExpenseMappings = Record<ExpenseType, string>;
export type IncomeMappings = Record<IncomeType, string>;

export interface Expense {
  date: string;
  expenseType: ExpenseType;
  amount: number;
  description: string;
}

export const ExpenseAccountMapping: Record<ExpenseType, string> = {
  Sekuru: `Entertainment/Director's Expenses`,
  Canteen: "Canteen",
  "Spoiled Meat": "Spoiled Meat",
  Utilities: "Utility Expenses",
  Consumables: "Consumables",
  "Neg Variance": "Variance Deficit",
  Other: "Miscellaneous Expenses",
};

export const IncomeAccountMapping: Record<IncomeType, string> = {
  "Pos Variance": "Variance Surplus",
  Cash: "Njeremoto Sales",
};

export interface Payment {
  id: string;
  date: string;
  status: "Draft" | "Submitted" | "Cancelled";
  type: "Expense" | "Order";
  description: string;
  amount: number;
}
