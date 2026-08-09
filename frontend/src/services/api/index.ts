import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

function createAuthFetch(): typeof globalThis.fetch {
  let tokenReady = false;

  return async (input: Request) => {
    const authStore = useAuthStore();

    const headers = new Headers(input.headers);
    if (!headers.has("Authorization")) {
      if (!authStore.accessToken && !tokenReady) {
        try {
          await authStore.refreshToken();
        } catch { /* let the 401 handler retry */ }
        tokenReady = true;
      }
      if (authStore.accessToken) {
        headers.set("Authorization", `Bearer ${authStore.accessToken}`);
      }
    }

    let req = new Request(input, { headers });
    let response = await fetch(req);

    if (response.status === 401 || response.status === 403) {
      try {
        await authStore.refreshToken();
      } catch {
        return response;
      }

      if (authStore.accessToken) {
        headers.set("Authorization", `Bearer ${authStore.accessToken}`);
        req = new Request(input, { headers });
        response = await fetch(req);
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

  public static reset() {
    this.instance = null;
  }
}
