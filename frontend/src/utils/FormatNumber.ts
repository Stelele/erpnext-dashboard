const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  useGrouping: false,
});

const decimalFormatter = new Intl.NumberFormat("en-ZW", {
  style: "decimal",
  maximumFractionDigits: 2,
  useGrouping: false,
});

function addSpaceGrouping(formatted: string): string {
  return formatted.replace(/\d+/g, (digits) =>
    digits.replace(/\B(?=(\d{3})+(?!\d))/g, " ")
  );
}

export function formatNumber(value: number, format: "decimal" | "currency") {
  if (format === "currency") {
    return addSpaceGrouping(currencyFormatter.format(value));
  }

  return addSpaceGrouping(decimalFormatter.format(value));
}
