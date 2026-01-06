import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { ApiSingleton } from "@/services/api";

export const useAuthStore = defineStore("authStore", () => {
  const company = computed(() => "Njeremoto Enterprises");

  const url = computed(() => {
    if (!user.value?.companies) {
      return import.meta.env.VITE_ERP_API_URL;
    }

    const userCompany = user.value.companies.find(
      (c) => c.name === company.value,
    );
    return userCompany?.site.url || "";
  });

  const token = computed(() => {
    if (!user.value?.companies) {
      return import.meta.env.VITE_ERP_API_TOKEN;
    }

    const userCompany = user.value?.companies?.find(
      (c) => c.name === company.value,
    );

    return userCompany?.site.apiToken || "";
  });

  const givenName = ref("");
  const email = ref("");
  const userId = ref("");
  const accessToken = ref("");
  const user = ref<components["schemas"]["ExtendedUserResponse"]>();

  async function update() {
    const { getAccessTokenSilently } = useAuth0();

    const token = await getAccessTokenSilently();
    accessToken.value = token;

    const payloadBase64 = token.split(".")[1] as string;
    const payload = JSON.parse(
      atob(payloadBase64.replace(/-/g, "+").replace(/_/g, "/")),
    );

    const nameSpace = "https://meta.dashboard.com/";
    const meta = payload[nameSpace];

    givenName.value = meta?.display_name || "Guest User";
    email.value = meta?.email || "";
    userId.value = meta?.user_id || "";

    try {
      const api = await ApiSingleton.getInstance();
      const { data } = await api.GET("/users/{id}", {
        params: { path: { id: userId.value } },
      });
      user.value = data;
    } catch (error) {
      console.error("Error fetching user data:", error);
    }
  }

  return {
    token,
    url,
    company,
    givenName,
    email,
    userId,
    accessToken,
    user,
    update,
  };
});
