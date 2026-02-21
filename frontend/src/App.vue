<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { useDataStore } from "./stores/DataStore";

const authStore = useAuthStore();
const dataStore = useDataStore();

onBeforeMount(async () => {
    await authStore.update();
    await dataStore.getData(dataStore.currentPeriod);
    setInterval(dataStore.getData, 5 * 60 * 1000, dataStore.currentPeriod);
});
</script>
