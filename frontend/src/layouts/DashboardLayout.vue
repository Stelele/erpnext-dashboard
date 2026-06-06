<template>
    <UApp>
        <UHeader>
            <template #left>
                <div class="flex items-center gap-2">
                    <!-- Mobile menu button -->
                    <UButton
                        variant="ghost"
                        color="neutral"
                        class="lg:hidden"
                        @click="drawerOpen = true"
                    >
                        <UIcon name="i-lucide-menu" class="w-5 h-5" />
                    </UButton>
                    <router-link to="/">
                        <div class="w-full h-full flex gap-2 items-center p-1">
                            <img
                                src="/logo.png"
                                class="object-contain h-20"
                                alt="Njeremoto Logo"
                            />
                        </div>
                    </router-link>
                </div>
            </template>
            <UPageHeader :title="`${authStore.company} Dashboard`" />
            <template #right>
                <div class="flex gap-1 m-1 items-center">
                    <!-- Mobile period button (mobile only) -->
                    <UButton
                        variant="subtle"
                        color="neutral"
                        class="lg:hidden"
                        @click="settingsDrawerOpen = true"
                    >
                        <div class="flex flex-col items-start">
                            <span class="text-sm font-bold leading-tight">{{
                                dataStore.currentPeriod
                            }}</span>
                            <span class="text-xs leading-tight text-muted">{{
                                dateRange
                            }}</span>
                        </div>
                    </UButton>
                    <!-- Desktop: avatar with logout dropdown -->
                    <UDropdownMenu :items="profileMenuItems">
                        <UAvatar
                            :alt="authStore.givenName"
                            size="lg"
                            class="hidden lg:flex hover:cursor-pointer"
                        />
                    </UDropdownMenu>
                </div>
            </template>
            <!-- Hide default mobile body menu and its trigger button -->
            <template #body>
                <div class="hidden lg:block" />
            </template>
        </UHeader>
        <UMain>
            <UDashboardGroup>
                <SideBar />
                <BasePage>
                    <UPageGrid>
                        <LoadingBar :loading="dataStore.loading" />
                        <slot />
                    </UPageGrid>
                </BasePage>
            </UDashboardGroup>
        </UMain>
        <!-- Mobile navigation drawer -->
        <UDrawer v-model:open="drawerOpen" direction="left">
            <template #header>
                <div class="flex items-center gap-2">
                    <img src="/logo.png" class="h-8 object-contain" alt="Logo" />
                    <h2 class="font-bold text-lg">{{ authStore.company }}</h2>
                </div>
            </template>
            <template #body>
                <div class="flex flex-col gap-1 p-2">
                    <UButton
                        v-for="item in navStore.navItems"
                        :key="item.label"
                        :variant="isActive(item.to) ? 'subtle' : 'ghost'"
                        :color="isActive(item.to) ? 'primary' : 'neutral'"
                        class="w-full justify-start min-h-[48px]"
                        :to="item.to"
                        @click="drawerOpen = false"
                    >
                        <UIcon :name="item.icon!" class="w-5 h-5 mr-3" />
                        <span class="font-medium">{{ item.label }}</span>
                    </UButton>
                </div>
            </template>
        </UDrawer>
        <!-- Mobile settings drawer -->
        <MobileSettingsDrawer v-model="settingsDrawerOpen" :user-name="authStore.givenName" />
    </UApp>
</template>

<script lang="ts" setup>
import { ref, computed } from "vue";
import { useRoute } from "vue-router";
import BasePage from "./BasePage.vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import moment from "moment";
import { useAuth0 } from "@auth0/auth0-vue";
import MobileSettingsDrawer from "@/components/MobileSettingsDrawer.vue";
import type { DropdownMenuItem } from "@nuxt/ui";

const route = useRoute();
const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();

const { logout } = useAuth0();

const drawerOpen = ref(false);
const settingsDrawerOpen = ref(false);

function isActive(to: string) {
    if (to === "/") return route.path === "/" || route.path === "/overview";
    return route.path.startsWith(to);
}

const profileMenuItems = computed<DropdownMenuItem[]>(() => [
    [
        {
            label: "Logout",
            icon: "i-lucide-log-out",
            onSelect: () =>
                logout({ logoutParams: { returnTo: window.location.origin } }),
        },
    ],
]);

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }

    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});

moment.updateLocale("en", {
    week: {
        dow: 1,
    },
});
</script>
