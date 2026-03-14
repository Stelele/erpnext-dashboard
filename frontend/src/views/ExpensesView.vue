ju36y7
<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                v-model:open="openBulkUpload"
                title="Bulk Add Expenses"
                :dismissible="false"
            >
                <UButton
                    trailing-icon="i-lucide-plus"
                    class="hover:cursor-pointer"
                    >Bulk Add Expenses</UButton
                >
                <template #body>
                    <BulkExpenseUploadButton
                        @on-data-extracted="onDataExtracted"
                        @error="onError"
                    />
                </template>
            </UModal>
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
            <UModal
                :fullscreen="true"
                v-model:open="openBulkPreview"
                title="Bulk Add Expenses Preview"
                :dismissible="false"
            >
                <template #body>
                    <BulkExpensePreview
                        :data="bulkPreviewData"
                        @onDataSubmit="onBulkSubmit"
                    />
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
import type { UniqueExpense } from "@/components/BulkExpenseUploadButton.vue";
import BulkExpensePreview from "@/components/BulkExpensePreview.vue";

const open = ref(false);
const openBulkUpload = ref(false);
const openBulkPreview = ref(false);
const bulkPreviewData = ref<UniqueExpense[]>([]);

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

function onDataExtracted(expenses: UniqueExpense[]) {
    bulkPreviewData.value = expenses;
    openBulkUpload.value = false;
    openBulkPreview.value = true;
}

async function onBulkSubmit(expenses: UniqueExpense[]) {
    const results = await dataStore.bulkAddDraftExpenses(expenses);

    const successCount = results.filter((r) => r.success).length;
    const failureCount = results.filter((r) => !r.success).length;

    if (successCount > 0) {
        toast.add({
            title: `Successfully submitted ${successCount} expenses`,
            color: "success",
        });
    }
    if (failureCount > 0) {
        toast.add({
            title: `Failed to submit ${failureCount} expenses`,
            color: "error",
        });
    }
    openBulkPreview.value = false;
}

function onError(error: string) {
    toast.add({
        title: `Error: ${error}`,
        color: "error",
    });
}
</script>
