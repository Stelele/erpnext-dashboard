import { defineStore } from "pinia";
import { computed } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { CachedApiClient } from "@/services/cache/CachedApiClient";

export const useFeatureStore = defineStore("featureStore", () => {
  const authStore = useAuthStore();

  let loadPromise: Promise<void> | null = null;
  let loaded = false;

  const features = computed<string[]>(
    () =>
      authStore.companies.find((c) => c.name === authStore.company)?.features ??
      [],
  );

  function hasFeature(code: string): boolean {
    return features.value.includes(code);
  }

  async function ensureLoaded(): Promise<void> {
    // Already have companies (e.g. AuthStore populated them) or a previous
    // load succeeded (even with an empty list): nothing to fetch.
    if (loaded || authStore.companies.length > 0 || !authStore.accessToken) {
      return;
    }
    if (!loadPromise) {
      loadPromise = (async () => {
        authStore.companies =
          await CachedApiClient.getInstance().getUserCompanies();
        // Restore the persisted selection BEFORE the router guard resolves
        // features: on a full page load the guard runs before
        // AuthStore.update(), and hasFeature() must check the company the
        // user actually selected, not the list default.
        const persisted = localStorage.getItem("selectedCompany");
        if (
          !authStore.selectedCompany &&
          persisted &&
          authStore.companies.some((c) => c.name === persisted)
        ) {
          authStore.selectedCompany = persisted;
        }
        // Any completed fetch counts as loaded — including an empty list —
        // so navigation never refetches in a loop. Only a thrown error
        // (cache/infra failure) leaves loaded false and allows a retry.
        loaded = true;
      })();
      try {
        await loadPromise;
      } finally {
        loadPromise = null;
      }
    } else {
      await loadPromise;
    }
  }

  return {
    features,
    hasFeature,
    ensureLoaded,
  };
});
