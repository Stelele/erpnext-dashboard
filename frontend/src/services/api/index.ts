import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

export class ApiSingleton {
  private static instance: Client | null = null;

  public static async getInstance() {
    if (this.instance) return this.instance;
    const authStore = useAuthStore();

    const api = createClient<paths>({
      baseUrl: import.meta.env.VITE_API_URL,
      headers: {
        Authorization: `Bearer ${authStore.accessToken}`,
      },
    });

    this.instance = api;

    return api;
  }
}
