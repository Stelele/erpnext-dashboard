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
                    <ExpenseForm
                        :mappings="mappings"
                        :loading="expenseLoading"
                        @on-submit="onSubmit"
                    />
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
                        :mappings="mappings"
                        :loading="bulkLoading"
                        @onDataSubmit="onBulkSubmit"
                    />
                </template>
            </UModal>
            <UModal
                v-model:open="openCancelConfirm"
                title="Cancel Purchase"
                :dismissible="false"
            >
                <template #body>
                    <div class="p-4 space-y-4">
                        <p class="text-sm">
                            Cancel Purchase
                            <strong>{{ pendingCancel?.id }}</strong
                            >?
                        </p>
                        <p class="text-sm text-[var(--ui-text)]">
                            This will cancel all linked documents: Purchase
                            Order, Purchase Receipt, Purchase Invoice, and
                            Payment Entry.
                            <strong>This action cannot be reversed.</strong>
                            Your stock levels will be reverted and accounting entries
                            reversed.
                        </p>
                        <div class="flex justify-end gap-2">
                            <UButton
                                color="neutral"
                                variant="outline"
                                :disabled="cancelLoading"
                                @click="openCancelConfirm = false"
                            >
                                Keep It
                            </UButton>
                            <UButton
                                color="error"
                                :loading="cancelLoading"
                                @click="confirmCancel"
                            >
                                Cancel Purchase
                            </UButton>
                        </div>
                    </div>
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
            @cancel="onCancelPurchase"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import type { Expense, CompanyExpenseMapping, Payment } from "@/types/Expenses";
import { useDataStore } from "@/stores/DataStore";
import { useExpenseDataStore } from "@/stores/ExpenseDataStore";
import { useAuthStore } from "@/stores/AuthStore";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import type { UniqueExpense } from "@/components/BulkExpenseUploadButton.vue";
import BulkExpensePreview from "@/components/BulkExpensePreview.vue";
import ExpenseForm from "@/components/ExpenseForm.vue";
import { ErpNextService } from "@/services/ErpNextService";

const open = ref(false);
const openBulkUpload = ref(false);
const openBulkPreview = ref(false);
const openCancelConfirm = ref(false);
const pendingCancel = ref<Payment | null>(null);
const cancelLoading = ref(false);
const expenseLoading = ref(false);
const bulkLoading = ref(false);
const bulkPreviewData = ref<UniqueExpense[]>([]);
const mappings = ref<CompanyExpenseMapping[]>([]);

const toast = useToast();
const dataStore = useDataStore();
const expenseDataStore = useExpenseDataStore();
const authStore = useAuthStore();
const erpnext = new ErpNextService();

const companyId = computed(() => {
    const companyName = authStore.company;
    return authStore.companies?.find((c) => c.name === companyName)?.id ?? "";
});

onMounted(async () => {
    if (companyId.value) {
        await loadCompanyData(companyId.value);
    }
});

watch(companyId, async (newId) => {
    if (newId) {
        await loadCompanyData(newId);
    }
});

async function loadCompanyData(id: string) {
    mappings.value = await dataStore.getCompanyExpenseMappings(id);
    const settings = await dataStore.getCompanySettings(id);
    await dataStore.initAccountMappings(
        mappings.value,
        settings?.defaultIncomeAccountName ?? "Sales",
    );
}

async function onSubmit(expense: Expense) {
    expenseLoading.value = true;
    const response = await dataStore.addDraftExpense(expense);
    expenseLoading.value = false;

    if (response) {
        open.value = false;
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
    bulkLoading.value = true;
    const results = await dataStore.bulkAddDraftExpenses(expenses);
    bulkLoading.value = false;

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

function onCancelPurchase(payment: Payment) {
    pendingCancel.value = payment;
    openCancelConfirm.value = true;
}

async function confirmCancel() {
    if (!pendingCancel.value) return;
    cancelLoading.value = true;
    const result = await erpnext.cancelFullPurchase(pendingCancel.value.id);

    if (result) {
        toast.add({
            title: `Purchase ${pendingCancel.value.id} cancelled`,
            color: "success",
        });
        await dataStore.update();
    } else {
        toast.add({
            title: `Failed to cancel purchase ${pendingCancel.value.id}`,
            color: "error",
        });
    }
    pendingCancel.value = null;
    cancelLoading.value = false;
    openCancelConfirm.value = false;
}
</script>
