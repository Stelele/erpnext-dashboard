<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { update } from "./utils/UpdateData";
import moment from "moment";

moment.updateLocale("en", {
    week: {
        dow: 1, // Monday is the first day of the week
    },
});

const authStore = useAuthStore();

onBeforeMount(async () => {
    console.log("Updating auth store...");
    await authStore.update();
    update();
});
</script>

