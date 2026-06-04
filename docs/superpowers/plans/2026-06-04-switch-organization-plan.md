# Switch Organization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a sidebar button and modal that lets users switch between their organizations, updating company name, logo, and re-fetching all dashboard data.

**Architecture:** Extend AuthStore with `selectedCompany` ref and `switchCompany()` method. Add a `CompanySwitcherModal.vue` component triggered from the sidebar footer. Logos fetched from ERPNext via a new static helper on ErpNextService. Data re-fetched via `dataStore.update()`. Selection persisted to localStorage.

**Tech Stack:** Vue 3, Pinia, TypeScript, Nuxt UI (v4), axios, ERPNext API

---

### Task 1: Add getCompanyLogo static helper to ErpNextService

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts`

- [ ] **Step 1: Add the static getCompanyLogo method**

Add this static method to the `ErpNextService` class (place it after the constructor, before `getSalesSummary`):

```typescript
  public static async getCompanyLogo(
    companyName: string,
    siteUrl: string,
    siteToken: string,
  ): Promise<string | undefined> {
    try {
      const response = await axios.get(
        `${siteUrl}/api/resource/Company/${encodeURIComponent(companyName)}`,
        {
          headers: {
            Authorization: `token ${siteToken}`,
          },
        },
      );
      return response.data.data?.company_logo;
    } catch {
      return undefined;
    }
  }
```

- [ ] **Step 2: Verify TypeScript compiles**

Run: `npx vue-tsc -b --noEmit` in `frontend/`
Expected: No errors related to ErpNextService.ts

- [ ] **Step 3: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add static getCompanyLogo helper to ErpNextService"
```

---

### Task 2: Add selectedCompany, switchCompany, and persistence to AuthStore

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts`

- [ ] **Step 1: Add selectedCompany ref and update company computed**

Replace the current `company` computed and add `selectedCompany` ref. The full updated `AuthStore.ts`:

```typescript
import { useAuth0 } from "@auth0/auth0-vue";
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { ApiSingleton } from "@/services/api";

const SELECTED_COMPANY_KEY = "selectedCompany";

export const useAuthStore = defineStore("authStore", () => {
  const selectedCompany = ref<string>("");

  const company = computed(() => {
    if (
      selectedCompany.value &&
      user.value?.companies?.find((c) => c.name === selectedCompany.value)
    ) {
      return selectedCompany.value;
    }
    return user.value?.companies?.[0]?.name || "Njeremoto Enterprises";
  });

  const url = computed(() => {
    const userCompany = user.value?.companies?.find(
      (c) => c.name === company.value,
    );
    return userCompany?.site.url || "";
  });

  const token = computed(() => {
    const userCompany = user.value?.companies?.find(
      (c) => c.name === company.value,
    );

    return userCompany?.site.apiToken || "";
  });

  const showSwitcher = computed(() => {
    return (user.value?.companies?.length || 0) > 2;
  });

  const givenName = ref("");
  const email = ref("");
  const userId = ref("");
  const accessToken = ref("");
  const user = ref<components["schemas"]["ExtendedUserResponse"]>();

  async function update() {
    const { getAccessTokenSilently } = useAuth0();

    const token = await getAccessTokenSilently();
    accessToken.value = token;

    const payloadBase64 = token.split(".")[1] as string;
    const payload = JSON.parse(
      atob(payloadBase64.replace(/-/g, "+").replace(/_/g, "/")),
    );

    const nameSpace = "https://meta.dashboard.com/";
    const meta = payload[nameSpace];

    givenName.value = meta?.display_name || "Guest User";
    email.value = meta?.email || "";
    userId.value = meta?.user_id || "";

    try {
      const api = await ApiSingleton.getInstance();
      const { data } = await api.GET("/users/{id}", {
        params: { path: { id: userId.value } },
      });
      user.value = data;

      // Restore persisted company selection
      const persisted = localStorage.getItem(SELECTED_COMPANY_KEY);
      if (persisted && data?.companies?.find((c) => c.name === persisted)) {
        selectedCompany.value = persisted;
      }
    } catch (error) {
      console.error("Error fetching user data:", error);
    }
  }

  async function switchCompany(
    companyName: string,
    onDataRefresh: () => Promise<void>,
  ) {
    const previous = selectedCompany.value;
    selectedCompany.value = companyName;
    localStorage.setItem(SELECTED_COMPANY_KEY, companyName);

    try {
      await onDataRefresh();
    } catch (error) {
      selectedCompany.value = previous;
      localStorage.setItem(SELECTED_COMPANY_KEY, previous);
      throw error;
    }
  }

  return {
    token,
    url,
    company,
    showSwitcher,
    givenName,
    email,
    userId,
    accessToken,
    user,
    selectedCompany,
    update,
    switchCompany,
  };
});
```

- [ ] **Step 2: Verify TypeScript compiles**

Run: `npx vue-tsc -b --noEmit` in `frontend/`
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add frontend/src/stores/AuthStore.ts
git commit -m "feat: add selectedCompany, switchCompany, and localStorage persistence to AuthStore"
```

---

### Task 3: Create CompanySwitcherModal component

**Files:**
- Create: `frontend/src/components/CompanySwitcherModal.vue`

- [ ] **Step 1: Create the CompanySwitcherModal component**

```vue
<template>
    <UModal v-model:open="isOpen" title="Switch Company" :dismissible="true">
        <UButton
            variant="subtle"
            color="neutral"
            class="w-full"
            @click="isOpen = true"
        >
            <div class="flex flex-col w-full">
                <CartTitle class="text-sm font-medium">Switch Company</CartTitle>
            </div>
        </UButton>
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
                    <div
                        v-if="logos[companyItem.name!] === undefined"
                        class="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-sm font-bold"
                    >
                        {{ companyItem.name?.charAt(0).toUpperCase() }}
                    </div>
                    <img
                        v-else-if="logos[companyItem.name!]"
                        :src="logos[companyItem.name!]"
                        :alt="companyItem.name"
                        class="w-10 h-10 rounded-full object-cover"
                    />
                    <div
                        v-else
                        class="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-sm font-bold"
                    >
                        {{ companyItem.name?.charAt(0).toUpperCase() }}
                    </div>
                    <div class="flex-1">
                        <div class="font-medium">{{ companyItem.name }}</div>
                        <div class="text-xs text-gray-500">
                            {{ companyItem.site.url }}
                        </div>
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
import { ref, watch } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { ErpNextService } from "@/services/ErpNextService";

const authStore = useAuthStore();
const dataStore = useDataStore();
const toast = useToast();

const isOpen = ref(false);
const logos = ref<Record<string, string | null>>({});

async function fetchLogos() {
    logos.value = {};
    const companies = authStore.user?.companies || [];

    const results = await Promise.allSettled(
        companies.map(async (companyItem) => {
            const name = companyItem.name!;
            const siteUrl = companyItem.site.url;
            const siteToken = companyItem.site.apiToken;
            if (!siteUrl || !siteToken) return { name, logo: null as string | null };

            const logo = await ErpNextService.getCompanyLogo(
                name,
                siteUrl,
                siteToken,
            );
            return { name, logo: logo ?? null };
        }),
    );

    for (const result of results) {
        if (result.status === "fulfilled") {
            logos.value[result.value.name] = result.value.logo;
        }
    }
}

watch(isOpen, (val) => {
    if (val) fetchLogos();
});

async function selectCompany(companyName: string) {
    if (companyName === authStore.company) {
        isOpen.value = false;
        return;
    }

    dataStore.loading = true;
    try {
        await authStore.switchCompany(companyName, async () => {
            await dataStore.update();
        });
        isOpen.value = false;
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

- [ ] **Step 2: Verify TypeScript compiles**

Run: `npx vue-tsc -b --noEmit` in `frontend/`
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/CompanySwitcherModal.vue
git commit -m "feat: add CompanySwitcherModal component with logo fetching"
```

---

### Task 4: Add switch button to Sidebar footer

**Files:**
- Modify: `frontend/src/components/SideBar.vue`

- [ ] **Step 1: Update SideBar.vue to include the switcher**

Replace the entire `SideBar.vue` content:

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
                <CompanySwitcherModal v-if="authStore.showSwitcher" />
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
import { useAuthStore } from "@/stores/AuthStore";
import { filterItems } from "@/utils/MenuItems";
import moment from "moment";

const authStore = useAuthStore();
const navStore = useNavStore();
const dataStore = useDataStore();

const dateRange = computed(() => {
    if (["Today", "Yesterday"].includes(dataStore.currentPeriod)) {
        return moment().format("DD MMM YY");
    }

    return `${moment(dataStore.dateRange.start).format("DD MMM YY")} - ${moment(dataStore.dateRange.end).format("DD MMM YY")}`;
});
</script>
```

- [ ] **Step 2: Verify TypeScript compiles**

Run: `npx vue-tsc -b --noEmit` in `frontend/`
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/SideBar.vue
git commit -m "feat: add CompanySwitcherModal to sidebar footer with visibility guard"
```

---

### Task 5: Final verification

- [ ] **Step 1: Full TypeScript check**

Run: `npx vue-tsc -b --noEmit` in `frontend/`
Expected: Zero errors

- [ ] **Step 2: Build check**

Run: `npm run build` in `frontend/`
Expected: Successful build with no errors

- [ ] **Step 3: Commit any remaining changes**

```bash
git add -A
git commit -m "chore: final verification and cleanup"
```
