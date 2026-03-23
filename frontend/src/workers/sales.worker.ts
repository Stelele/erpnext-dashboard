import moment from "moment";
import type { SalesDetail } from "../types/SalesDetail";

moment.updateLocale("en", {
  week: {
    dow: 1, // Monday is the first day of the week
  },
});

interface SerializedData {
  sales: SalesDetail[];
}

export interface SalesResults {
  salesDetails: SalesDetail[];
  mobileSalesDetails: Record<string, SalesDetail[]>;
  mobileSalesDateDetails: string[];
}

function computeSales(data: SerializedData): SalesResults {
  const salesDetails = computeSalesDetails(data.sales);
  const mobileSalesDetails = computeMobileSalesDetails(salesDetails);
  const mobileSalesDateDetails =
    computeMobileSalesDateDetails(mobileSalesDetails);

  return {
    salesDetails,
    mobileSalesDetails,
    mobileSalesDateDetails,
  };
}

function computeSalesDetails(sales: SalesDetail[]): SalesDetail[] {
  const temp: SalesDetail[] = [];
  for (const saleDetail of sales) {
    const entry = temp.find((item) => {
      console.log(item.posting_date, saleDetail.posting_date);
      const itemDate = moment(item.posting_date, "YYYY-MM-DD").format(
        "DD MMM YYYY",
      );
      const saleDate = moment(saleDetail.posting_date, "YYYY-MM-DD").format(
        "DD MMM YYYY",
      );
      return (
        itemDate === saleDate &&
        item.item_name === saleDetail.item_name &&
        item.item_group === saleDetail.item_group
      );
    });

    if (!entry) {
      temp.push(saleDetail);
    } else {
      entry.qty += saleDetail.qty;
      entry.item_total_amount += saleDetail.item_total_amount;
    }
  }

  return temp.sort((a, b) => {
    const aDate = moment(a.posting_date, "DD MMM YYYY");
    const bDate = moment(b.posting_date, "DD MMM YYYY");

    if (aDate.isSame(bDate, "days")) {
      return a.item_group.localeCompare(b.item_group);
    }

    return aDate.diff(bDate, "days");
  });
}

function computeMobileSalesDetails(
  salesDetails: SalesDetail[],
): Record<string, SalesDetail[]> {
  const data: Record<string, SalesDetail[]> = {};
  for (const saleDetail of salesDetails) {
    if (!(saleDetail.posting_date in data)) {
      data[saleDetail.posting_date] = [];
    }

    data[saleDetail.posting_date]?.push(saleDetail);
  }

  return data;
}

function computeMobileSalesDateDetails(
  mobileSalesDetails: Record<string, SalesDetail[]>,
): string[] {
  return Object.keys(mobileSalesDetails).sort((a, b) => {
    const aDate = moment(a, "DD MMM YYYY");
    const bDate = moment(b, "DD MMM YYYY");
    return aDate.diff(bDate, "days");
  });
}

self.onmessage = (e: MessageEvent<SerializedData>) => {
  const results = computeSales(e.data);
  self.postMessage(results);
};
