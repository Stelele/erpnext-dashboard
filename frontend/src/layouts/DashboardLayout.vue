<template>
    <UApp>
        <UHeader>
            <template #left>
                <router-link to="/">
                    <div class="w-full h-full flex gap-2 items-center p-1">
                        <img
                            src="/logo.png"
                            class="object-contain h-20"
                            alt="Njeremoto Logo"
                        />
                    </div>
                </router-link>
            </template>
            <UPageHeader :title="`${authStore.company} Dashboard`" />
            <template #right>
                <div class="flex gap-1 m-1 items-center">
                    <UDropdownMenu
                        :items="filterItems"
                        class="w-full visible lg:hidden"
                    >
                        <UButton
                            variant="subtle"
                            color="neutral"
                            class="w-full"
                        >
                            <div class="flex flex-col">
                                <CartTitle class="text-md font-bold">{{
                                    dataStore.currentPeriod
                                }}</CartTitle>
                                <CartTitle class="text-sm">{{
                                    dateRange
                                }}</CartTitle>
                            </div>
                        </UButton>
                    </UDropdownMenu>
                    <UColorModeButton class="hover:cursor-pointer" />
                    <UDropdownMenu :items="profileMenuItems">
                        <UAvatar
                            :alt="authStore.givenName"
                            size="lg"
                            class="hover:cursor-pointer"
                        />
                    </UDropdownMenu>
                </div>
            </template>
            <template #body>
                <UNavigationMenu
                    :items="navStore.navItems"
                    orientation="vertical"
                    class="-mx-2.5"
                />
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
    </UApp>
</template>

<script lang="ts" setup>
import { computed, onBeforeMount } from "vue";
import BasePage from "./BasePage.vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import { filterItems } from "@/utils/MenuItems";
import moment from "moment";
import type { DropdownMenuItem } from "@nuxt/ui";
import { useAuth0 } from "@auth0/auth0-vue";

const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();

const { logout } = useAuth0();

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }

    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});

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

moment.updateLocale("en", {
    week: {
        dow: 1, // Monday is the first day of the week.
    },
});

onBeforeMount(async () => {
    await authStore.update();
    await dataStore.getData(dataStore.currentPeriod);
});
</script>
