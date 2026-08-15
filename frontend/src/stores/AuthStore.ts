import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { CachedApiClient } from "@/services/cache/CachedApiClient";
import { ApiSingleton } from "@/services/api";
import { getCacheDB } from "@/services/db";

const SELECTED_COMPANY_KEY = "selectedCompany";
const TOKEN_KEY = "authToken";
const USER_KEY = "authUser";

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
  }
}

function safeRemoveItem(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch {
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
  const accessToken = ref(safeGetItem(TOKEN_KEY) ?? "");

  let _loggingOut = false;

  async function loadSiteData(siteId: string) {
    const client = CachedApiClient.getInstance();
    const site = await client.getSite(siteId);
    if (site) {
      siteUrl.value = site.url;
      siteToken.value = site.apiToken;
    }
  }

  async function fetchLogoUrl(siteId: string, companyName: string): Promise<string> {
    const cacheKey = `${siteId}:${companyName}`;
    if (logoUrls.value[cacheKey]) return logoUrls.value[cacheKey];

    const client = CachedApiClient.getInstance();
    const url = await client.getSiteLogo(siteId, companyName);
    logoUrls.value[cacheKey] = url;
    return url;
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

  function storeSession(token: string, u: components["schemas"]["UserResponse"]) {
    accessToken.value = token;
    safeSetItem(TOKEN_KEY, token);
    safeSetItem(USER_KEY, JSON.stringify(u));
    givenName.value = u.name;
    email.value = u.email;
    userId.value = u.id;
  }

  function clearSession() {
    accessToken.value = "";
    safeRemoveItem(TOKEN_KEY);
    safeRemoveItem(USER_KEY);
    givenName.value = "";
    email.value = "";
    userId.value = "";
    selectedCompany.value = "";
    safeRemoveItem(SELECTED_COMPANY_KEY);
  }

  async function login(loginEmail: string, password: string) {
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.POST("/auth/login", {
      body: { email: loginEmail, password },
    });
    if (error || !data) throw new Error("Invalid email or password.");
    storeSession(data.token, data.user);
    await update();
  }

  async function update() {
    const stored = safeGetItem(TOKEN_KEY);
    if (!stored) {
      clearSession();
      return;
    }
    accessToken.value = stored;

    try {
      const client = CachedApiClient.getInstance();
      const me = await client.getCurrentUser();
      if (!me) {
        clearSession();
        return;
      }
      storeSession(stored, me);

      if (me.companies?.length) {
        companies.value = await client.getUserCompanies();

        const persisted = safeGetItem(SELECTED_COMPANY_KEY);
        if (persisted && companies.value.find((c) => c.name === persisted)) {
          selectedCompany.value = persisted;
        }

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
      console.error("Error restoring session:", error);
    }
  }

  async function triggerLogout() {
    if (_loggingOut) return;
    _loggingOut = true;

    const currentToken = accessToken.value;
    if (currentToken) {
      try {
        const api = await ApiSingleton.getInstance();
        await api.POST("/auth/logout", {});
      } catch {
      }
    }

    clearSession();

    try {
      await getCacheDB().delete();
      await getCacheDB().open();
    } catch {
    }

    window.location.href = "/login";
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
    selectedCompany,
    update,
    switchCompany,
    login,
    triggerLogout,
    storeSession,
    clearSession,
  };
});
