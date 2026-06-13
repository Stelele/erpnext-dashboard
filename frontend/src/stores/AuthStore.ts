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
  const companies = ref<components["schemas"]["CompanyResponse"][]>([]);
  const siteUrl = ref("");
  const siteToken = ref("");
  const logo = ref("/logo.png");
  const logoUrls = ref<Record<string, string>>({});

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

  async function fetchLogoUrl(siteId: string, companyName: string): Promise<string> {
    const cacheKey = `${siteId}:${companyName}`;
    if (logoUrls.value[cacheKey]) return logoUrls.value[cacheKey];

    try {
      const api = await ApiSingleton.getInstance();
      const { data } = await api.GET("/sites/{siteId}/logo", {
        params: { path: { siteId }, query: { company: companyName } },
      });
      const url = data?.url || "/logo.png";
      logoUrls.value[cacheKey] = url;
      return url;
    } catch {
      return "/logo.png";
    }
  }

  async function loadCurrentLogo() {
    const currentCompany = companies.value.find((c) => c.name === company.value);
    if (currentCompany?.siteId) {
      logo.value = await fetchLogoUrl(currentCompany.siteId, company.value);
    }
  }

  async function loadAllLogos() {
    await Promise.all(
      companies.value
        .filter((c) => c.siteId)
        .map((c) => fetchLogoUrl(c.siteId, c.name)),
    );
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

        // Restore persisted company selection before loading data
        const persisted = safeGetItem(SELECTED_COMPANY_KEY);
        if (persisted && companies.value.find((c) => c.name === persisted)) {
          selectedCompany.value = persisted;
        }

        // Load site data for the selected (or first) company
        const selected = companies.value.find(
          (c) => c.name === selectedCompany.value,
        ) ?? companies.value[0];
        if (selected) {
          await Promise.all([
            (async () => { await loadCurrentLogo(); await loadAllLogos(); })(),
            loadSiteData(selected.siteId),
          ]);
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
      await Promise.all([
        loadCurrentLogo(),
        loadSiteData(selected.siteId),
      ]);
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
    logoUrls,
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
