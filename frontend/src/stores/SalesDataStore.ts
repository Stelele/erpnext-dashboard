import type { SalesDetail } from "@/types/SalesDetail";
import { defineStore } from "pinia";
import { ref } from "vue";

interface AggregatedSale {
  posting_date: string;
  item_name: string;
  item_group: string;
  qty: number;
  item_total_amount: number;
}

export const useSalesDataStore = defineStore("salesDataStore", () => {
  const salesDetails = ref<SalesDetail[]>([]);
  const mobileSalesDetails = ref<Record<string, SalesDetail[]>>({});
  const mobileSalesDateDetails = ref<string[]>([]);

  function applyAggregatedSales(data: AggregatedSale[]) {
    salesDetails.value = data.map(sale => ({
      pos_invoice_id: "",
      posting_date: sale.posting_date,
      item_name: sale.item_name,
      item_group: sale.item_group,
      qty: sale.qty,
      rate: Math.round(sale.item_total_amount / (sale.qty || 1) * 100) / 100,
      item_total_amount: sale.item_total_amount,
    }));

    const mobile: Record<string, SalesDetail[]> = {};
    salesDetails.value.forEach(sale => {
      if (!(sale.posting_date in mobile)) {
        mobile[sale.posting_date] = [];
      }
      mobile[sale.posting_date]?.push(sale);
    });

    mobileSalesDetails.value = mobile;
    mobileSalesDateDetails.value = Object.keys(mobile).sort((a, b) => {
      return new Date(b).getTime() - new Date(a).getTime();
    });
  }

  return {
    salesDetails,
    mobileSalesDetails,
    mobileSalesDateDetails,
    applyAggregatedSales,
  };
});
