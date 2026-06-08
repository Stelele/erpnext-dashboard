<template>
    <div class="flex flex-col gap-3 p-2">
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
                v-if="logos[companyItem.name]"
                :src="logos[companyItem.name]"
                :alt="companyItem.name"
                class="w-10 h-10 rounded-full object-cover"
                @error="logos[companyItem.name] = null"
            />
            <div
                v-else
                class="w-10 h-10 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center text-sm font-bold"
            >
                {{ companyItem.name.charAt(0).toUpperCase() }}
            </div>
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
    </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useAuthStore } from "@/stores/AuthStore";
import { useDataStore } from "@/stores/DataStore";
import { ErpNextService } from "@/services/ErpNextService";
import { ApiSingleton } from "@/services/api";

const authStore = useAuthStore();
const dataStore = useDataStore();
const toast = useToast();

const isOpen = defineModel<boolean>({ default: false });
const logos = ref<Record<string, string | null>>({});
const siteUrls = ref<Record<string, string>>({});

async function fetchLogos() {
    logos.value = {};
    siteUrls.value = {};
    const companies = authStore.companies;

    const results = await Promise.allSettled(
        companies.map(async (companyItem) => {
            const name = companyItem.name;
            const siteId = companyItem.siteId;
            if (!siteId) return { id: companyItem.id, name, logo: null as string | null, url: '' };

            const api = await ApiSingleton.getInstance();
            const { data: site } = await api.GET("/sites/{id}", {
                params: { path: { id: siteId } },
            });
            if (!site) return { id: companyItem.id, name, logo: null as string | null, url: '' };

            const logo = await ErpNextService.getCompanyLogo(
                name,
                site.url,
                site.apiToken,
            );
            return { id: companyItem.id, name, logo: logo ?? null, url: site.url };
        }),
    );

    for (const result of results) {
        if (result.status === "fulfilled") {
            logos.value[result.value.name] = result.value.logo;
            if (result.value.url) {
                siteUrls.value[result.value.id] = result.value.url;
            }
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
    dataStore.clear();
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
