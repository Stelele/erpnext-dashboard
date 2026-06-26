function addSpaceGrouping(formatted: string): string {
  return formatted.replace(/\d+/g, (digits) =>
    digits.replace(/\B(?=(\d{3})+(?!\d))/g, " ")
  );
}

export function formatNumber(value: number, format: "decimal" | "currency") {
  if (format === "currency") {
    return addSpaceGrouping(
      new Intl.NumberFormat("en-US", {
        style: "currency",
        currency: "USD",
        useGrouping: false,
      }).format(value)
    );
  }

  return addSpaceGrouping(
    new Intl.NumberFormat("en-ZW", {
      style: "decimal",
      maximumFractionDigits: 3,
      useGrouping: false,
    }).format(value)
  );
}
