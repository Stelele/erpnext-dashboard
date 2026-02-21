import type { SalesDetail } from "@/types/SalesDetail";
import moment from "moment";
import { defineStore } from "pinia";
import { ref } from "vue";
import { useDataStore } from "./DataStore";

export const useSalesDataStore = defineStore("salesDataStore", () => {
  const dataStore = useDataStore();
  const salesDetails = ref<SalesDetail[]>([]);
  const mobileSalesDetails = ref<Record<string, SalesDetail[]>>({});
  const mobileSalesDateDetails = ref<string[]>([]);

  function update() {
    salesDetails.value = getSalesDetails();
    mobileSalesDetails.value = getMobileSalesDetails();
    mobileSalesDateDetails.value = getMobileSalesDateDetails();

    function getSalesDetails() {
      const temp: SalesDetail[] = [];
      for (const saleDetail of dataStore.sales) {
        const entry = temp.find((item) => {
          const itemDate = moment(item.posting_date).format("DD MMM YYYY");
          const saleDate = moment(saleDetail.posting_date).format(
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

        return aDate.isBefore(bDate, "days") ? -1 : 1;
      });
    }

    function getMobileSalesDetails() {
      const data: Record<string, SalesDetail[]> = {};
      for (const saleDetail of salesDetails.value) {
        if (!(saleDetail.posting_date in data)) {
          data[saleDetail.posting_date] = [];
        }

        data[saleDetail.posting_date]?.push(saleDetail);
      }

      return data;
    }

    function getMobileSalesDateDetails() {
      return Object.keys(mobileSalesDetails.value).sort((a, b) => {
        const aDate = moment(a, "DD MMM YYYY");
        const bDate = moment(b, "DD MMM YYYY");
        return aDate.isBefore(bDate, "days") ? -1 : 1;
      });
    }
  }

  return {
    salesDetails,
    mobileSalesDetails,
    mobileSalesDateDetails,
    update,
  };
});
