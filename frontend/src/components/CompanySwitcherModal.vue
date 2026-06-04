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
                    <img
                        v-if="logos[companyItem.name!]"
                        :src="logos[companyItem.name!]!"
                        :alt="companyItem.name"
                        class="w-10 h-10 rounded-full object-cover"
                        @error="logos[companyItem.name!] = null"
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
                            {{ companyItem.site?.url }}
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
