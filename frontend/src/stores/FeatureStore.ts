import { defineStore } from "pinia";
import { computed } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { CachedApiClient } from "@/services/cache/CachedApiClient";

export const useFeatureStore = defineStore("featureStore", () => {
  const authStore = useAuthStore();

  let loadPromise: Promise<void> | null = null;

  const features = computed<string[]>(
    () =>
      authStore.companies.find((c) => c.name === authStore.company)?.features ??
      [],
  );

  function hasFeature(code: string): boolean {
    return features.value.includes(code);
  }

  async function ensureLoaded(): Promise<void> {
    if (!authStore.accessToken || loadPromise) return loadPromise ?? undefined;
    loadPromise = (async () => {
      authStore.companies =
        await CachedApiClient.getInstance().getUserCompanies();
    })();
    try {
      await loadPromise;
    } finally {
      // A genuinely empty company list must not refetch on every navigation,
      // but a failed load should allow a retry.
      loadPromise = authStore.companies.length > 0 ? loadPromise : null;
    }
  }

  return {
    features,
    hasFeature,
    ensureLoaded,
  };
});
