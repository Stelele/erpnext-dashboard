<template>
    <DashboardLayout>
        <UModal
            v-model:open="open"
            title="Create New Expense"
            :dismissible="false"
        >
            <UButton
                trailing-icon="i-lucide-plus"
                class="hover:cursor-pointer w-fit col-end-7"
                >Add Expense</UButton
            >

            <template #body>
                <ExpenseForm @on-submit="onSubmit" />
            </template>
        </UModal>
        <ExpenseTable
            :data="dataStore.paymentEntries"
            :loading="dataStore.loading"
        />
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
