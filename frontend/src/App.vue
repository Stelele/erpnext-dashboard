<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount, computed } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import { useCompanyTheme } from "./composables/useCompanyTheme";
import { useHead } from "@unhead/vue";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});

const authStore = useAuthStore();

useHead(
    computed(() => ({
        title: authStore.company
            ? `${authStore.company} Dashboard`
            : "njeremoto-dashboard",
        link: [
            { rel: "icon", href: authStore.logo, type: "image/x-icon" },
            { rel: "shortcut icon", href: authStore.logo, type: "image/x-icon" },
        ],
    })),
);

const { loadAndApply } = useCompanyTheme();

onBeforeMount(async () => {
    await authStore.update();
    update();

    const currentCompany = authStore.companies.find(
        (c) => c.name === authStore.company
    );
    if (currentCompany) {
        await loadAndApply(currentCompany.id);
    }
});
</script>

