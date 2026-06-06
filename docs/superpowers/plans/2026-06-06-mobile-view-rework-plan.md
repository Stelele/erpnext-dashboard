# Mobile View Rework Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace mobile period selector with a right-sliding UDrawer containing period list, company switcher, and logout, while improving hit targets on desktop.

**Architecture:** Add `UDrawer` component for mobile (< 640px) triggered from header. Desktop keeps sidebar with taller period rows and company switcher in footer. Extract shared period list into a reusable component.

**Tech Stack:** Vue 3, Nuxt UI v4, TypeScript, Pinia

---

### File Structure

| File | Action | Responsibility |
|------|--------|----------------|
| `frontend/src/components/PeriodSelector.vue` | Create | Reusable period list component (used in both sidebar and drawer) |
| `frontend/src/components/MobileSettingsDrawer.vue` | Create | UDrawer with period selector, company switcher, logout |
| `frontend/src/layouts/DashboardLayout.vue` | Modify | Add mobile period button, integrate drawer |
| `frontend/src/components/SideBar.vue` | Modify | Use PeriodSelector, add company switcher, taller rows |
| `frontend/src/utils/MenuItems.ts` | No change | `filterItems` reused by PeriodSelector |

---

### Task 1: Create PeriodSelector Component

**Files:**
- Create: `frontend/src/components/PeriodSelector.vue`

- [ ] **Step 1: Create PeriodSelector component**

This component renders the current period display and a list of period options. It will be used in both the sidebar (desktop) and the drawer (mobile).

```vue
<template>
    <div class="flex flex-col gap-1">
        <div class="mb-2">
            <div class="bg-primary/10 rounded-lg p-3">
                <div class="font-bold text-sm">{{ dataStore.currentPeriod }}</div>
                <div class="text-xs text-muted">{{ dateRange }}</div>
            </div>
        </div>
        <div class="flex flex-col gap-1">
            <UButton
                v-for="item in filterItems"
                :key="item.label"
                :variant="dataStore.currentPeriod === item.label ? 'subtle' : 'ghost'"
                :color="dataStore.currentPeriod === item.label ? 'primary' : 'neutral'"
                class="w-full justify-start min-h-[44px]"
                @click="selectPeriod(item)"
            >
                {{ item.label }}
            </UButton>
        </div>
    </div>
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

function selectPeriod(item: DropdownMenuItem) {
    if (item.onSelect) {
        item.onSelect();
    }
}
</script>
```

- [ ] **Step 2: Verify component builds**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No type errors

---

### Task 2: Create MobileSettingsDrawer Component

**Files:**
- Create: `frontend/src/components/MobileSettingsDrawer.vue`

- [ ] **Step 1: Create MobileSettingsDrawer component**

This drawer slides from the right on mobile and contains period selector, company switcher, and logout.

```vue
<template>
    <UDrawer v-model:open="open" direction="right">
        <template #header>
            <div class="flex items-center justify-between w-full">
                <h2 class="font-bold text-lg">Settings</h2>
                <UButton variant="ghost" color="neutral" @click="open = false">
                    <UIcon name="i-lucide-x" class="w-5 h-5" />
                </UButton>
            </div>
        </template>
        <template #body>
            <div class="flex flex-col gap-4 p-4">
                <div>
                    <div class="text-xs uppercase text-muted mb-2">Reporting Period</div>
                    <PeriodSelector />
                </div>
                <div v-if="authStore.showSwitcher" class="border-t pt-4">
                    <UButton
                        variant="subtle"
                        color="success"
                        class="w-full min-h-[44px]"
                        @click="openCompanyModal = true"
                    >
                        <UIcon name="i-lucide-building-2" class="w-4 h-4" />
                        Switch Company
                    </UButton>
                </div>
                <div class="border-t pt-4">
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
            <div class="flex flex-col gap-3 p-2">
                <div
                    v-for="companyItem in authStore.user?.companies"
                    :key="companyItem.id"
                    @click="selectCompany(companyItem.name!)"
                    class="flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors"
                    :class="
                        companyItem.name === authStore.company
                            ? 'bg-primary/10 ring-1 ring-primary'
                            : 'hover:bg-gray-100 dark:hover:bg-gray-800'
                    "
                >
                    <div class="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-sm font-bold">
                        {{ companyItem.name?.charAt(0).toUpperCase() }}
                    </div>
                    <div class="flex-1">
                        <div class="font-medium">{{ companyItem.name }}</div>
                        <div class="text-xs text-gray-500">{{ companyItem.site?.url }}</div>
                    </div>
                    <UIcon
                        v-if="companyItem.name === authStore.company"
                        name="i-lucide-check"
                        class="w-5 h-5 text-primary"
                    />
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import PeriodSelector from "./PeriodSelector.vue";

const authStore = useAuthStore();
const dataStore = useDataStore();
const toast = useToast();
const { logout } = useAuth0();

const open = defineModel<boolean>({ default: false });
const openCompanyModal = ref(false);

async function handleLogout() {
    open.value = false;
    logout({ logoutParams: { returnTo: window.location.origin } });
}

async function selectCompany(companyName: string) {
    if (companyName === authStore.company) {
        openCompanyModal.value = false;
        return;
    }

    dataStore.loading = true;
    dataStore.clear();
    try {
        await authStore.switchCompany(companyName, async () => {
            await dataStore.update();
        });
        openCompanyModal.value = false;
        toast.add({
            title: `Switched to ${companyName}`,
            color: "success",
        });
    } catch {
        toast.add({
            title: `Failed to switch to ${companyName}`,
            color: "error",
        });
    } finally {
        dataStore.loading = false;
    }
}
</script>
```

- [ ] **Step 2: Verify component builds**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No type errors

---

### Task 3: Update DashboardLayout to Include Drawer

**Files:**
- Modify: `frontend/src/layouts/DashboardLayout.vue`

- [ ] **Step 1: Add mobile period button and drawer to DashboardLayout**

Replace the current mobile dropdown and add the drawer component. The full updated file:

```vue
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
                    <!-- Mobile period button -->
                    <UButton
                        variant="subtle"
                        color="neutral"
                        class="lg:hidden"
                        @click="drawerOpen = true"
                    >
                        <div class="flex flex-col">
                            <CartTitle class="text-sm font-bold">{{
                                dataStore.currentPeriod
                            }}</CartTitle>
                            <CartTitle class="text-xs">{{
                                dateRange
                            }}</CartTitle>
                        </div>
                    </UButton>
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
        <!-- Mobile settings drawer -->
        <MobileSettingsDrawer v-model="drawerOpen" />
    </UApp>
</template>

<script lang="ts" setup>
import { ref, computed } from "vue";
import BasePage from "./BasePage.vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import moment from "moment";
import type { DropdownMenuItem } from "@nuxt/ui";
import { useAuth0 } from "@auth0/auth0-vue";
import MobileSettingsDrawer from "@/components/MobileSettingsDrawer.vue";

const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();

const { logout } = useAuth0();

const drawerOpen = ref(false);

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
        dow: 1,
    },
});
</script>
```

Key changes:
- Removed the mobile `UDropdownMenu` for period selection (was lines 18-36)
- Added `UButton` with `lg:hidden` class to show only on mobile
- Added `MobileSettingsDrawer` component with `v-model` binding
- Added `drawerOpen` ref state

- [ ] **Step 2: Verify component builds**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No type errors

---

### Task 4: Update SideBar for Desktop

**Files:**
- Modify: `frontend/src/components/SideBar.vue`

- [ ] **Step 1: Update SideBar to use PeriodSelector and add company switcher**

Replace the sidebar footer to use the new PeriodSelector component and add company switcher. Remove the old dropdown-based period selector.

```vue
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
                        @click="openCompanyModal = true"
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
    <UModal v-model:open="openCompanyModal" title="Switch Company" :dismissible="true">
        <template #body>
            <div class="flex flex-col gap-3 p-2">
                <div
                    v-for="companyItem in authStore.user?.companies"
                    :key="companyItem.id"
                    @click="selectCompany(companyItem.name!)"
                    class="flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors"
                    :class="
                        companyItem.name === authStore.company
                            ? 'bg-primary/10 ring-1 ring-primary'
                            : 'hover:bg-gray-100 dark:hover:bg-gray-800'
                    "
                >
                    <div class="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-sm font-bold">
                        {{ companyItem.name?.charAt(0).toUpperCase() }}
                    </div>
                    <div class="flex-1">
                        <div class="font-medium">{{ companyItem.name }}</div>
                        <div class="text-xs text-gray-500">{{ companyItem.site?.url }}</div>
                    </div>
                    <UIcon
                        v-if="companyItem.name === authStore.company"
                        name="i-lucide-check"
                        class="w-5 h-5 text-primary"
                    />
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useDataStore } from "@/stores/DataStore";
import { useNavStore } from "@/stores/NavStore";
import { useAuthStore } from "@/stores/AuthStore";
import { useToast } from "#imports";
import PeriodSelector from "./PeriodSelector.vue";

const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();
const toast = useToast();

const openCompanyModal = ref(false);

async function selectCompany(companyName: string) {
    if (companyName === authStore.company) {
        openCompanyModal.value = false;
        return;
    }

    dataStore.loading = true;
    dataStore.clear();
    try {
        await authStore.switchCompany(companyName, async () => {
            await dataStore.update();
        });
        openCompanyModal.value = false;
        toast.add({
            title: `Switched to ${companyName}`,
            color: "success",
        });
    } catch {
        toast.add({
            title: `Failed to switch to ${companyName}`,
            color: "error",
        });
    } finally {
        dataStore.loading = false;
    }
}
</script>
```

Key changes:
- Removed `UDropdownMenu` with `filterItems` for period selection
- Added `PeriodSelector` component (handles period display and selection)
- Added company switcher button that opens a modal
- All clickable elements have `min-h-[44px]` for better hit targets
- Removed old `CartTitle` period display and `dateRange` computed

- [ ] **Step 2: Verify component builds**

Run: `cd frontend && npx vue-tsc --noEmit`
Expected: No type errors

---

### Task 5: Remove Old CompanySwitcherModal Usage

**Files:**
- Modify: `frontend/src/components/SideBar.vue` (already done in Task 4 - company switcher is now inline)

- [ ] **Step 1: Verify no other references to old CompanySwitcherModal in sidebar**

The old `CompanySwitcherModal` was referenced in the sidebar with `v-if="authStore.showSwitcher"`. Task 4 replaced this with an inline modal. Verify the old import is removed.

Run: `cd frontend && grep -r "CompanySwitcherModal" src/`
Expected: Only references should be in the original `CompanySwitcherModal.vue` file itself (if it's still used elsewhere) or none at all.

- [ ] **Step 2: Run full build to verify**

Run: `cd frontend && npm run build`
Expected: Build succeeds with no errors

---

### Task 6: Manual Testing Checklist

- [ ] **Step 1: Test mobile view (< 640px)**

1. Open dev tools, set viewport to 375x667 (iPhone SE)
2. Verify header shows period button, theme toggle, avatar
3. Tap period button - drawer slides from right
4. Verify drawer shows: current period, 7 period options, company switcher (if applicable), logout
5. Tap a period - drawer closes, data refreshes
6. Tap company switcher - modal opens with company list
7. Tap logout - redirects to Auth0 logout

- [ ] **Step 2: Test desktop view (>= 1024px)**

1. Set viewport to 1280x800
2. Verify sidebar is visible with navigation
3. Verify sidebar footer shows: period selector (taller rows), company switcher (if applicable), last refresh time
4. Verify logout is still in avatar dropdown (not in sidebar)
5. Tap a period in sidebar - data refreshes
6. Verify all clickable areas are at least 44px tall

- [ ] **Step 3: Test tablet view (640px - 1023px)**

1. Set viewport to 768x1024
2. Verify sidebar is visible
3. Verify same behavior as desktop
