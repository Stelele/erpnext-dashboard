<template>
    <div class="flex flex-col gap-3 p-2">
        <template v-if="!switchingCompany">
            <template v-for="companyItem in authStore.companies" :key="companyItem.id">
                <div
                    v-if="companyItem.name"
                    @click="selectCompany(companyItem.name)"
                    class="flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors"
                    :class="
                        companyItem.name === authStore.company
                            ? 'bg-primary/10 ring-1 ring-primary'
                            : 'hover:bg-gray-100 dark:hover:bg-gray-800'
                    "
                >
                <img
                    :src="cachedLogoUrl(companyItem.siteId, companyItem.name)"
                    :alt="companyItem.name"
                    class="w-10 h-10 rounded-full object-cover"
                />
                <div class="flex-1">
                    <div class="font-medium">{{ companyItem.name }}</div>
                    <div class="text-xs text-gray-500">
                        {{ siteUrls[companyItem.id] ?? '' }}
                    </div>
                </div>
                <UIcon
                    v-if="companyItem.name === authStore.company"
                    name="i-lucide-check"
                    class="w-5 h-5 text-primary"
                />
            </div>
            </template>
        </template>
        <div v-else class="flex flex-col items-center justify-center py-8 gap-4">
            <UIcon name="i-lucide-loader-2" class="w-8 h-8 animate-spin text-primary" />
            <span class="text-sm text-gray-500">Switching to {{ switchingCompany }}...</span>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { useCompanyTheme } from "@/composables/useCompanyTheme";

const authStore = useAuthStore();
const dataStore = useDataStore();
const toast = useToast();
const { loadAndApply } = useCompanyTheme();

const isOpen = defineModel<boolean>({ default: false });
const siteUrls = ref<Record<string, string>>({});
const switchingCompany = ref<string | null>(null);

function cachedLogoUrl(siteId: string, companyName: string): string {
  const cacheKey = `${siteId}:${companyName}`;
  return authStore.logoUrls[cacheKey] || "/logo.png";
}


async function selectCompany(companyName: string) {
    if (companyName === authStore.company) {
        isOpen.value = false;
        return;
    }

    switchingCompany.value = companyName;
    dataStore.loading = true;
    dataStore.clear();
    try {
        await authStore.switchCompany(companyName, async () => {
            await dataStore.update();
        });

        const newCompany = authStore.companies.find((c) => c.name === companyName);
        if (newCompany) {
            await loadAndApply(newCompany.id);
        }

        isOpen.value = false;
        toast.add({
            title: `Switched to ${companyName}`,
            color: "success",
        });
    } catch {
        switchingCompany.value = null;
        toast.add({
            title: `Failed to switch to ${companyName}`,
            color: "error",
        });
    } finally {
        dataStore.loading = false;
        if (isOpen.value) {
            switchingCompany.value = null;
        }
    }
}
</script>
