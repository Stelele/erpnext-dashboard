import { ref, onUnmounted } from "vue";
import { useAuthStore } from "@/stores/AuthStore";

const syncProgress = ref(0);
const syncTotal = ref(0);
const isSyncing = ref(false);
const lastSync = ref<string | null>(null);
const syncError = ref<string | null>(null);

let worker: Worker | null = null;

export function useCacheSync() {
  function startSync() {
    if (worker) return;

    worker = new Worker(
      new URL("@/services/cache/cacheSyncWorker.ts", import.meta.url),
      { type: "module" },
    );

    worker.onmessage = (event: MessageEvent) => {
      const { type, current, total, context, error } = event.data;
      switch (type) {
        case "SYNC_PROGRESS":
          syncProgress.value = current;
          syncTotal.value = total;
          isSyncing.value = true;
          break;
        case "SYNC_COMPLETE":
          isSyncing.value = false;
          syncError.value = null;
          lastSync.value = new Date().toISOString();
          break;
        case "SYNC_ERROR":
          syncError.value = `[${context}] ${error}`;
          break;
      }
    };

    const authStore = useAuthStore();
    worker.postMessage({
      type: "SET_CONFIG",
      token: authStore.accessToken,
      url: import.meta.env.VITE_API_URL,
    });
    worker.postMessage({ type: "START_SYNC" });
  }

  function stopSync() {
    worker?.postMessage({ type: "STOP_SYNC" });
    worker?.terminate();
    worker = null;
  }

  function updateToken(token: string) {
    worker?.postMessage({ type: "SET_CONFIG", token, url: import.meta.env.VITE_API_URL });
  }

  onUnmounted(() => {
    stopSync();
  });

  return {
    syncProgress,
    syncTotal,
    isSyncing,
    lastSync,
    syncError,
    startSync,
    stopSync,
    updateToken,
  };
}
