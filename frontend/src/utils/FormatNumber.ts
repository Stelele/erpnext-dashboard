export function formatNumber(value: number, format: "decimal" | "currency") {
  if (format === "currency") {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: "USD",
    }).format(value);
  }

  return new Intl.NumberFormat("en-ZW", {
    style: "decimal",
    maximumFractionDigits: 3,
  }).format(value);
}
