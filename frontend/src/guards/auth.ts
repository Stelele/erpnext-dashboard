import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "@/stores/AuthStore";

export function authGuard(to: RouteLocationNormalized) {
  const authStore = useAuthStore();

  if (to.meta.public) {
    return authStore.accessToken ? { path: "/" } : true;
  }

  return authStore.accessToken ? true : { path: "/login" };
}