export const FEATURES = {
  sales: "sales",
  stock: "stock",
  expenses: "expenses",
} as const;

export type FeatureCode = (typeof FEATURES)[keyof typeof FEATURES];
