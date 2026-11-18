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
    await featureStore.ensureLoaded();
    if (!featureStore.hasFeature(to.meta.feature as string)) {
      return { path: "/" };
    }
  }

  return true;
}
