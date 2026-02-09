<template>
    <DashboardLayout>
        <StockTable
            :data="dataStore.stockDetails"
            :loading="dataStore.loading"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import { useDataStore } from "@/stores/DataStore";

const dataStore = useDataStore();
</script>
