export const FEATURES = {
  sales: "sales",
  stock: "stock",
  expense: "expense",
  refund: "refund",
} as const;

export type FeatureCode = (typeof FEATURES)[keyof typeof FEATURES];
