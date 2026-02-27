<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse">
            <UModal
                v-model:open="open"
                title="Create New Expense"
                :dismissible="false"
            >
                <UButton
                    trailing-icon="i-lucide-plus"
                    class="hover:cursor-pointer"
                    >Add Expense</UButton
                >

                <template #body>
                    <ExpenseForm @on-submit="onSubmit" />
                </template>
            </UModal>
        </div>
        <CardDoughnutChart
            title="Orders By Suppliers"
            :data="expenseDataStore.orderBreakdown"
        />
        <CardDoughnutChart
            title="Expenses By Type"
            :data="expenseDataStore.expenseBreakdown"
        />
        <CardBarChart
            title="Expenses from last 6 months"
            :data="expenseDataStore.prev6MonthsExpenses"
        />
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
import { useExpenseDataStore } from "@/stores/ExpenseDataStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";

const open = ref(false);

//@ts-ignore
const toast = useToast();
const dataStore = useDataStore();
const expenseDataStore = useExpenseDataStore();

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
