import { defineStore } from "pinia";
import { computed } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { CachedApiClient } from "@/services/cache/CachedApiClient";

export const useFeatureStore = defineStore("featureStore", () => {
  const authStore = useAuthStore();

  const features = computed<string[]>(
    () =>
      authStore.companies.find((c) => c.name === authStore.company)?.features ??
      [],
  );

  function hasFeature(code: string): boolean {
    return features.value.includes(code);
  }

  async function ensureLoaded(): Promise<void> {
    if (authStore.accessToken && authStore.companies.length === 0) {
      authStore.companies =
        await CachedApiClient.getInstance().getUserCompanies();
    }
  }

  return {
    features,
    hasFeature,
    ensureLoaded,
  };
});
