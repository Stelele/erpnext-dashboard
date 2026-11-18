import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "@/stores/AuthStore";
import { useFeatureStore } from "@/stores/FeatureStore";

export async function authGuard(to: RouteLocationNormalized) {
  const authStore = useAuthStore();

  if (to.meta.public) {
    return authStore.accessToken ? { path: "/" } : true;
  }

  if (!authStore.accessToken) {
    return { path: "/login" };
  }

  if (to.meta.feature) {
    const featureStore = useFeatureStore();
    try {
      await featureStore.ensureLoaded();
    } catch (error) {
      // Fail open: the backend still gates feature-scoped data; a broken
      // cache/API must not strand navigation. Matches AuthStore's
      // catch-and-continue style.
      console.error("Feature load failed during navigation:", error);
      return true;
    }
    if (!featureStore.hasFeature(to.meta.feature)) {
      return { path: "/" };
    }
  }

  return true;
}
