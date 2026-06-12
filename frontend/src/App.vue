<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import { useCompanyTheme } from "./composables/useCompanyTheme";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});

const authStore = useAuthStore();
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

