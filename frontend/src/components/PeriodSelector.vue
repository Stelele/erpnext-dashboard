<template>
    <UDropdownMenu :items="dropdownItems" :ui="{ item: 'min-h-[44px]' }">
        <UButton variant="subtle" color="neutral" class="w-full justify-between">
            <div class="flex flex-col items-start">
                <span class="text-sm font-bold">{{ dataStore.currentPeriod }}</span>
                <span class="text-xs text-muted">{{ dateRange }}</span>
            </div>
            <UIcon name="i-lucide-chevron-down" class="w-4 h-4 text-muted" />
        </UButton>
    </UDropdownMenu>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useDataStore } from "@/stores/DataStore";
import { filterItems } from "@/utils/MenuItems";
import type { DropdownMenuItem } from "@nuxt/ui";
import moment from "moment";

const dataStore = useDataStore();

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }
    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});

const dropdownItems = computed<DropdownMenuItem[]>(() => [
    filterItems.value.map(item => ({
        ...item,
        type: "item" as const,
        color: dataStore.currentPeriod === item.label ? "primary" : "neutral",
        icon: dataStore.currentPeriod === item.label ? "i-lucide-check" : undefined,
        class: "min-h-[44px]",
    })),
]);
</script>
