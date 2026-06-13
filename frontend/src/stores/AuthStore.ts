import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { ApiSingleton } from "@/services/api";
import { getLogoProxyUrl } from "@/services/api/logo";

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
  const companies = ref<components["schemas"]["CompanyResponse"][]>([]);
  const siteUrl = ref("");
  const siteToken = ref("");
  const logo = ref("/logo.png");

  const company = computed(() => {
    if (
      selectedCompany.value &&
      companies.value?.find((c) => c.name === selectedCompany.value)
    ) {
      return selectedCompany.value;
    }
    return companies.value?.[0]?.name || "";
  });

  const url = computed(() => siteUrl.value);
  const token = computed(() => siteToken.value);

  const showSwitcher = computed(() => {
    return companies.value.length > 1;
  });

  const givenName = ref("");
  const email = ref("");
  const userId = ref("");
  const accessToken = ref("");
  const user = ref<components["schemas"]["UserResponse"]>();

  async function loadSiteData(siteId: string) {
    const api = await ApiSingleton.getInstance();
    const { data: site } = await api.GET("/sites/{id}", {
      params: { path: { id: siteId } },
    });
    if (site) {
      siteUrl.value = site.url;
      siteToken.value = site.apiToken;
    }
  }

  function loadCurrentLogo() {
    const currentCompany = companies.value.find((c) => c.name === company.value);
    if (currentCompany?.siteId) {
      logo.value = getLogoProxyUrl(currentCompany.siteId, company.value);
    }
  }

  async function update() {
    const { getAccessTokenSilently } = useAuth0();

    const apiToken = await getAccessTokenSilently();
    accessToken.value = apiToken;

    const payloadBase64 = apiToken.split(".")[1] as string;
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

      // Fetch company details to get site IDs
      if (data?.companies?.length) {
        const companyResponses = await Promise.all(
          data.companies.map((id) =>
            api.GET("/companies/{id}", { params: { path: { id } } }),
          ),
        );
        companies.value = companyResponses
          .map((r) => r.data)
          .filter((c): c is components["schemas"]["CompanyResponse"] => !!c);

        // Load site data for the selected (or first) company
        const selected = companies.value.find(
          (c) => c.name === selectedCompany.value,
        ) ?? companies.value[0];
        if (selected) {
          loadCurrentLogo();
          await loadSiteData(selected.siteId);
        }
      }

      // Restore persisted company selection
      const persisted = safeGetItem(SELECTED_COMPANY_KEY);
      if (persisted && companies.value.find((c) => c.name === persisted)) {
        selectedCompany.value = persisted;
        // Reload site data if we restored a different company
        const restored = companies.value.find((c) => c.name === persisted);
        if (restored && restored.siteId !== companies.value[0]?.siteId) {
          loadCurrentLogo();
          await loadSiteData(restored.siteId);
        }
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

    const selected = companies.value.find((c) => c.name === companyName);
    if (selected) {
      loadCurrentLogo();
      await loadSiteData(selected.siteId);
    }

    try {
      await onDataRefresh();
    } catch (error) {
      selectedCompany.value = previous;
      safeSetItem(SELECTED_COMPANY_KEY, previous);
      throw error;
    }
  }

  return {
    companies,
    token,
    url,
    logo,
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
