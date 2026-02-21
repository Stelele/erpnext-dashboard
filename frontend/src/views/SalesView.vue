<template>
    <DashboardLayout>
        <SalesTable
            :loading="dataStore.loading"
            :salesDetails="salesDataStore.salesDetails"
            :mobileSalesDetails="salesDataStore.mobileSalesDetails"
            :mobileSalesDateDetails="salesDataStore.mobileSalesDateDetails"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import { useDataStore } from "@/stores/DataStore";
import { useSalesDataStore } from "@/stores/SalesDataStore";

const dataStore = useDataStore();
const salesDataStore = useSalesDataStore();
</script>
