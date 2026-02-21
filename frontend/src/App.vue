<template>
    <RouterView />
</template>

<script setup lang="ts">
import { onBeforeMount } from "vue";
import { useAuthStore } from "./stores/AuthStore";
import { useDataStore } from "./stores/DataStore";
import { useOverViewDataStore } from "./stores/OverViewDataStore";
import { useSalesDataStore } from "./stores/SalesDataStore";
import { useStockDataStore } from "./stores/StockDataStore";

const authStore = useAuthStore();
const dataStore = useDataStore();
const overViewDataStore = useOverViewDataStore();
const salesDataStore = useSalesDataStore();
const stockDataStore = useStockDataStore();

onBeforeMount(async () => {
    await authStore.update();
    update();
    setInterval(update, 5 * 60 * 1000);
});

async function update() {
    await dataStore.update();
    overViewDataStore.update();
    salesDataStore.update();
    stockDataStore.update();
}
</script>
