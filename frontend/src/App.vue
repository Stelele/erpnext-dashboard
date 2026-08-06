<template>
    <div
      v-if="isOffline"
      class="offline-banner"
    >
      <span class="offline-banner-icon">&#9888;</span>
      You are offline. Some features may be unavailable.
    </div>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount, computed, ref, watch } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import { useCompanyTheme } from "./composables/useCompanyTheme";
import { useCacheSync } from "./composables/useCacheSync";
import { CachedApiClient } from "./services/cache/CachedApiClient";
import { ApiSingleton } from "./services/api";
import { useHead } from "@unhead/vue";
import { useAuth0 } from "@auth0/auth0-vue";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});

const authStore = useAuthStore();
const { startSync, updateToken } = useCacheSync();
const { loadAndApply, currentPrimaryColor } = useCompanyTheme();
const { error, logout } = useAuth0();

const isOffline = ref(!navigator.onLine);

function handleOnline() {
  isOffline.value = false;
}

function handleOffline() {
  isOffline.value = true;
}

window.addEventListener("online", handleOnline);
window.addEventListener("offline", handleOffline);

watch(error, (err) => {
  if (err?.error === "login_required") {
    ApiSingleton.reset();
    logout({ logoutParams: { returnTo: window.location.origin } });
  }
});

watch(() => authStore.accessToken, (token) => {
  if (token) {
    updateToken(token);
  }
});

function buildManifestJson(name: string, iconSrc: string, themeColor: string): string {
    const manifest = {
        name: `${name} Dashboard`,
        short_name: name,
        start_url: "/",
        scope: "/",
        display: "standalone" as const,
        theme_color: themeColor,
        background_color: themeColor,
        icons: [
            {
                src: iconSrc,
                sizes: "192x192",
                type: "image/png",
                purpose: "any maskable",
            },
            {
                src: iconSrc,
                sizes: "512x512",
                type: "image/png",
                purpose: "any maskable",
            },
        ],
    };
    return JSON.stringify(manifest);
}

const manifestBlobUrl = ref<string | null>(null);

watch(
    [() => authStore.company, () => authStore.logo, currentPrimaryColor],
    ([company, logo, themeColor]) => {
        if (manifestBlobUrl.value) {
            URL.revokeObjectURL(manifestBlobUrl.value);
            manifestBlobUrl.value = null;
        }
        if (company && logo) {
            const json = buildManifestJson(company, logo, themeColor);
            const blob = new Blob([json], { type: "application/manifest+json" });
            manifestBlobUrl.value = URL.createObjectURL(blob);
        }
    },
    { immediate: true },
);

useHead(
    computed(() => ({
        title: authStore.company
            ? `${authStore.company} Dashboard`
            : "njeremoto-dashboard",
        meta: [
            {
                name: "theme-color",
                content: currentPrimaryColor.value,
            },
            {
                name: "apple-mobile-web-app-title",
                content: authStore.company || "njeremoto-dashboard",
            },
        ],
        link: [
            { rel: "icon", href: authStore.logo, type: "image/x-icon" },
            { rel: "shortcut icon", href: authStore.logo, type: "image/x-icon" },
            { rel: "apple-touch-icon", href: authStore.logo },
            ...(manifestBlobUrl.value
                ? [{ rel: "manifest", href: manifestBlobUrl.value }]
                : []),
        ],
    })),
);

onBeforeMount(async () => {
    await authStore.update();

    const cacheClient = CachedApiClient.getInstance();
    await cacheClient.init();
    await cacheClient.bootstrap(authStore.userId);

    startSync();
    update();

    const currentCompany = authStore.companies.find(
        (c) => c.name === authStore.company
    );
    if (currentCompany) {
        await loadAndApply(currentCompany.id);
    }
});
</script>

<style>
.offline-banner {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 9999;
  background: #f59e0b;
  color: #1e1e1e;
  text-align: center;
  padding: 8px 16px;
  font-size: 14px;
  font-weight: 500;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.offline-banner-icon {
  font-size: 16px;
}
</style>

