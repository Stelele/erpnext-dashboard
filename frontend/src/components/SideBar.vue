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
                <PeriodSelector />
                <div v-if="authStore.showSwitcher" class="border-t pt-2 mt-2">
                    <UButton
                        variant="subtle"
                        color="success"
                        class="w-full min-h-[44px]"
                        @click="showCompanyModal = true"
                    >
                        <UIcon name="i-lucide-building-2" class="w-4 h-4" />
                        <span v-if="!collapsed">Switch Company</span>
                    </UButton>
                </div>
                <CartTitle class="text-sm"
                    >Last Refresh: {{ dataStore.lastRefresh }}</CartTitle
                >
            </div>
        </template>
    </UDashboardSidebar>
    <UModal v-model:open="showCompanyModal" title="Switch Company" :dismissible="true">
        <template #body>
            <CompanySwitcherModalContent v-model="showCompanyModal" />
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import { useAuthStore } from "@/stores/AuthStore";
import PeriodSelector from "./PeriodSelector.vue";
import CompanySwitcherModalContent from "./CompanySwitcherModalContent.vue";
import CartTitle from "./CartTitle.vue";

const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();

const showCompanyModal = ref(false);
</script>
