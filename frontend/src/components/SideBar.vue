<template>
    <UDashboardSidebar
        collapsible
        class="pt-16"
        :ui="{ footer: 'border-t border-default' }"
    >
        <template #default="{ collapsed }">
            <UNavigationMenu
                :collapsed="collapsed"
                :items="navStore.navItems"
                orientation="vertical"
            />
        </template>
        <template #footer="{ collapsed }">
            <div class="flex flex-col gap-1">
                <UDropdownMenu
                    :collapsed="collapsed"
                    :items="filterItems"
                    class="w-full"
                >
                    <UButton variant="subtle" color="neutral" class="w-full">
                        <div class="flex flex-col w-full">
                            <CartTitle class="text-lg font-bold">{{
                                dataStore.currentPeriod
                            }}</CartTitle>
                            <CartTitle class="text-sm"
                                >{{ dateRange }}
                            </CartTitle>
                        </div>
                    </UButton>
                </UDropdownMenu>
                <CartTitle class="text-sm"
                    >Last Refresh: {{ dataStore.lastRefresh }}</CartTitle
                >
            </div>
        </template>
    </UDashboardSidebar>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import { filterItems } from "@/utils/MenuItems";
import moment from "moment";

const navStore = useNavStore();
const dataStore = useDataStore();

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }

    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});
</script>
