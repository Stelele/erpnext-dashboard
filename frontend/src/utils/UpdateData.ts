import { useDataStore } from "@/stores/DataStore";
import { useExpenseDataStore } from "@/stores/ExpenseDataStore";
import { useOverViewDataStore } from "@/stores/OverViewDataStore";
import { useSalesDataStore } from "@/stores/SalesDataStore";
import { useStockDataStore } from "@/stores/StockDataStore";

let timeOutId: NodeJS.Timeout | undefined;

export async function update() {
  if (timeOutId) {
    clearTimeout(timeOutId);
    timeOutId = undefined;
  }

  await useDataStore().update();
  useOverViewDataStore().update();
  useSalesDataStore().update();
  useStockDataStore().update();
  useExpenseDataStore().update();

  timeOutId = setTimeout(update, 5 * 60 * 1000);
}
