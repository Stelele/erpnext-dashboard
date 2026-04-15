import { useDataStore } from "@/stores/DataStore";

let timeOutId: NodeJS.Timeout | undefined;

export async function update() {
  const dataStore = useDataStore();
  dataStore.loading = true;

  if (timeOutId) {
    clearTimeout(timeOutId);
    timeOutId = undefined;
  }

  await dataStore.update();

  dataStore.loading = false;

  timeOutId = setTimeout(update, 5 * 60 * 1000);
}

export function stopAutoRefresh() {
  if (timeOutId) {
    clearTimeout(timeOutId);
    timeOutId = undefined;
  }
}
