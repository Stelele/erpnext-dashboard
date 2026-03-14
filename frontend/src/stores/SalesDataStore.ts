import type { SalesDetail } from "@/types/SalesDetail";
import { defineStore } from "pinia";
import { ref } from "vue";
import type { SalesResults } from "../workers/sales.worker";

export const useSalesDataStore = defineStore("salesDataStore", () => {
  const salesDetails = ref<SalesDetail[]>([]);
  const mobileSalesDetails = ref<Record<string, SalesDetail[]>>({});
  const mobileSalesDateDetails = ref<string[]>([]);

  function applyResults(results: SalesResults) {
    salesDetails.value = results.salesDetails;
    mobileSalesDetails.value = results.mobileSalesDetails;
    mobileSalesDateDetails.value = results.mobileSalesDateDetails;
  }

  return {
    salesDetails,
    mobileSalesDetails,
    mobileSalesDateDetails,
    applyResults,
  };
});
