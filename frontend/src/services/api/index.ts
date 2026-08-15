import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

function createAuthFetch(): typeof globalThis.fetch {
  let redirecting = false;

  return async (input: RequestInfo | URL, init?: RequestInit) => {
    const authStore = useAuthStore();

    const source =
      input instanceof Request ? input : new Request(input, init);
    const headers = new Headers(source.headers);
    if (!headers.has("Authorization") && authStore.accessToken) {
      headers.set("Authorization", `Bearer ${authStore.accessToken}`);
    }

    const response = await fetch(source, { ...init, headers });

    if (response.status === 401 && !redirecting && headers.has("Authorization")) {
      redirecting = true;
      authStore.clearSession();
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }

    return response;
  };
}

export class ApiSingleton {
  private static instance: Client | null = null;

  public static async getInstance() {
    if (this.instance) return this.instance;

    const api = createClient<paths>({
      baseUrl: import.meta.env.VITE_API_URL,
      fetch: createAuthFetch(),
    });

    this.instance = api;

    return api;
  }
}
