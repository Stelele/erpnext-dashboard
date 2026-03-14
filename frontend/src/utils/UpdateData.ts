import { useDataStore } from "@/stores/DataStore";
import { useExpenseDataStore } from "@/stores/ExpenseDataStore";
import { useOverViewDataStore } from "@/stores/OverViewDataStore";
import { useSalesDataStore } from "@/stores/SalesDataStore";
import { useStockDataStore } from "@/stores/StockDataStore";
import type { OverviewResults } from "@/workers/overview.worker";
import type { SalesResults } from "@/workers/sales.worker";
import type { StockResults } from "@/workers/stock.worker";
import type { ExpenseResults } from "@/workers/expense.worker";

let timeOutId: NodeJS.Timeout | undefined;

const overviewWorker = new Worker(
  new URL("../workers/overview.worker.ts", import.meta.url),
  { type: "module" },
);

const salesWorker = new Worker(
  new URL("../workers/sales.worker.ts", import.meta.url),
  { type: "module" },
);

const stockWorker = new Worker(
  new URL("../workers/stock.worker.ts", import.meta.url),
  { type: "module" },
);

const expenseWorker = new Worker(
  new URL("../workers/expense.worker.ts", import.meta.url),
  { type: "module" },
);

function runWorker<T>(worker: Worker, data: unknown): Promise<T> {
  return new Promise((resolve) => {
    worker.onmessage = (e: MessageEvent<T>) => {
      resolve(e.data);
    };
    worker.postMessage(data);
  });
}

export async function update() {
  const dataStore = useDataStore();
  dataStore.loading = true;

  if (timeOutId) {
    clearTimeout(timeOutId);
    timeOutId = undefined;
  }

  await dataStore.update();
  const serializedData = dataStore.toJson();

  const overviewData = {
    salesSummary: serializedData.salesSummary,
    prevSalesSummary: serializedData.prevSalesSummary,
    prevXGroupingSales: serializedData.prevXGroupingSales,
    prev6MonthsSales: serializedData.prev6MonthsSales,
    purchaseGroupSummary: serializedData.purchaseGroupSummary,
    prevPurchaseGroupSummary: serializedData.prevPurchaseGroupSummary,
    expensesSummary: serializedData.expensesSummary,
    prevExpensesSummary: serializedData.prevExpensesSummary,
    itemGroupSalesSummary: serializedData.itemGroupSalesSummary,
    currentPeriod: serializedData.currentPeriod,
  };

  const salesData = {
    sales: serializedData.sales,
  };

  const stockData = {
    stockValues: serializedData.stockValues,
    salesStockValues: serializedData.salesStockValues,
    currentPeriod: serializedData.currentPeriod,
    stockDetails: serializedData.stockDetails,
    dailyStockValues: serializedData.dailyStockValues,
  };

  const expenseData = {
    paymentEntries: serializedData.paymentEntries,
    accountMappings: serializedData.accountMappings,
    prev6MonthsExpenses: serializedData.prev6MonthsExpenses,
  };

  const [overviewResults, salesResults, stockResults, expenseResults] =
    await Promise.all([
      runWorker<OverviewResults>(overviewWorker, overviewData),
      runWorker<SalesResults>(salesWorker, salesData),
      runWorker<StockResults>(stockWorker, stockData),
      runWorker<ExpenseResults>(expenseWorker, expenseData),
    ]);

  useOverViewDataStore().applyResults(overviewResults);
  useSalesDataStore().applyResults(salesResults);
  useStockDataStore().applyResults(stockResults);
  useExpenseDataStore().applyResults(expenseResults);

  dataStore.loading = false;

  timeOutId = setTimeout(update, 5 * 60 * 1000);
}
