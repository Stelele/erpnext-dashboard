import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { ApiSingleton } from "@/services/api";

const SELECTED_COMPANY_KEY = "selectedCompany";

function safeGetItem(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeSetItem(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Ignore in private browsing / storage disabled
  }
}

export const useAuthStore = defineStore("authStore", () => {
  const selectedCompany = ref<string>("");

  const company = computed(() => {
    if (
      selectedCompany.value &&
      user.value?.companies?.find((c) => c.name === selectedCompany.value)
    ) {
      return selectedCompany.value;
    }
    return user.value?.companies?.[0]?.name || "Njeremoto Enterprises";
  });

  const url = computed(() => {
    const userCompany = user.value?.companies?.find(
      (c) => c.name === company.value,
    );
    return userCompany?.site.url || "";
  });

  const token = computed(() => {
    const userCompany = user.value?.companies?.find(
      (c) => c.name === company.value,
    );

    return userCompany?.site.apiToken || "";
  });

  const showSwitcher = computed(() => {
    return (user.value?.companies?.length || 0) > 1;
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

      // Restore persisted company selection
      const persisted = safeGetItem(SELECTED_COMPANY_KEY);
      if (persisted && data?.companies?.find((c) => c.name === persisted)) {
        selectedCompany.value = persisted;
      }
    } catch (error) {
      console.error("Error fetching user data:", error);
    }
  }

  async function switchCompany(
    companyName: string,
    onDataRefresh: () => Promise<void>,
  ) {
    const previous = selectedCompany.value;
    selectedCompany.value = companyName;
    safeSetItem(SELECTED_COMPANY_KEY, companyName);

    try {
      await onDataRefresh();
    } catch (error) {
      selectedCompany.value = previous;
      safeSetItem(SELECTED_COMPANY_KEY, previous);
      throw error;
    }
  }

  return {
    token,
    url,
    company,
    showSwitcher,
    givenName,
    email,
    userId,
    accessToken,
    user,
    selectedCompany,
    update,
    switchCompany,
  };
});
