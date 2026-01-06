<template>
    <DashboardLayout>
        <div class="w-full h-full flex items-center justify-center">
            <UModal
                v-model:open="open"
                title="Create New Expense"
                :dismissible="false"
            >
                <UButton
                    trailing-icon="i-lucide-plus"
                    size="md"
                    class="hover:cursor-pointer"
                    >Add Expense</UButton
                >

                <template #body>
                    <ExpenseForm @on-submit="onSubmit" />
                </template>
            </UModal>
        </div>
    </DashboardLayout>
</template>

<script setup lang="ts">
import { ref } from "vue";
import type { Expense } from "@/types/Expenses";
import { useDataStore } from "@/stores/DataStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const open = ref(false);

//@ts-ignore
const toast = useToast();
const dataStore = useDataStore();

async function onSubmit(expense: Expense) {
    open.value = false;
    const response = await dataStore.addDraftExpense(expense);

    if (response) {
        toast.add({
            title: `Expense submitted successfully: ${response.name}`,
            color: "success",
        });
    } else {
        toast.add({
            title: "Failed to submit expense",
            color: "error",
        });
    }
}
</script>
