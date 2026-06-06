<template>
    <UDrawer v-model:open="open" direction="right">
        <template #header>
            <div class="flex items-center gap-3">
                <UAvatar :alt="userName" size="sm" />
                <div>
                    <div class="font-bold text-sm">{{ userName }}</div>
                    <div class="text-xs text-muted">{{ authStore.email }}</div>
                </div>
            </div>
        </template>
        <template #body>
            <div class="flex flex-col h-full p-4">
                <div>
                    <div class="text-xs uppercase text-muted mb-2">Reporting Period</div>
                    <div class="bg-primary/10 rounded-lg p-3 mb-2">
                        <div class="font-bold text-sm">{{ dataStore.currentPeriod }}</div>
                        <div class="text-xs text-muted">{{ dateRange }}</div>
                    </div>
                    <div class="flex flex-col gap-1">
                        <UButton
                            v-for="item in filterItems"
                            :key="item.label"
                            :variant="dataStore.currentPeriod === item.label ? 'subtle' : 'ghost'"
                            :color="dataStore.currentPeriod === item.label ? 'primary' : 'neutral'"
                            class="w-full justify-start min-h-[44px]"
                            @click="item.onSelect?.()"
                        >
                            {{ item.label }}
                        </UButton>
                    </div>
                </div>
                <div class="mt-auto pt-4 border-t flex flex-col gap-2">
                    <UButton
                        v-if="authStore.showSwitcher"
                        variant="subtle"
                        color="success"
                        class="w-full min-h-[44px]"
                        @click="openCompanyModal = true"
                    >
                        <UIcon name="i-lucide-building-2" class="w-4 h-4" />
                        Switch Company
                    </UButton>
                    <UButton
                        variant="subtle"
                        color="error"
                        class="w-full min-h-[44px]"
                        @click="handleLogout"
                    >
                        <UIcon name="i-lucide-log-out" class="w-4 h-4" />
                        Logout
                    </UButton>
                </div>
            </div>
        </template>
    </UDrawer>
    <UModal v-model:open="openCompanyModal" title="Switch Company" :dismissible="true">
        <template #body>
            <CompanySwitcherModalContent v-model="openCompanyModal" />
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import { useAuth0 } from "@auth0/auth0-vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { filterItems } from "@/utils/MenuItems";
import CompanySwitcherModalContent from "./CompanySwitcherModalContent.vue";
import moment from "moment";

defineProps<{
    userName: string;
}>();

const authStore = useAuthStore();
const dataStore = useDataStore();
const { logout } = useAuth0();

const open = defineModel<boolean>({ default: false });
const openCompanyModal = ref(false);

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }
    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});

function handleLogout() {
    open.value = false;
    logout({ logoutParams: { returnTo: window.location.origin } });
}
</script>
