import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

function createAuthFetch(): typeof globalThis.fetch {
  return async (input: Request) => {
    const authStore = useAuthStore();

    const headers = new Headers(input.headers);
    if (!headers.has("Authorization") && authStore.accessToken) {
      headers.set("Authorization", `Bearer ${authStore.accessToken}`);
    }

    const req = new Request(input, { headers });

    let response = await fetch(req);

    if (response.status === 401 || response.status === 403) {
      try {
        await authStore.refreshToken();
      } catch {
        return response;
      }

      headers.set("Authorization", `Bearer ${authStore.accessToken || ""}`);
      const retryReq = new Request(input, { headers });
      response = await fetch(retryReq);
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
