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
                :title="pendingCancel?.type === 'Expense' ? 'Cancel Expense' : 'Cancel Purchase'"
                :dismissible="false"
            >
                <template #body>
                    <div class="p-4 space-y-4">
                        <p class="text-sm">
                            {{ pendingCancel?.type === 'Expense' ? 'Cancel Expense' : 'Cancel Purchase' }}
                            <strong>{{ pendingCancel?.id }}</strong
                            >?
                        </p>
                        <p v-if="pendingCancel?.type === 'Order'" class="text-sm text-[var(--ui-text)]">
                            This will cancel all linked documents: Purchase
                            Order, Purchase Receipt, Purchase Invoice, and
                            Payment Entry.
                            <strong>This action cannot be reversed.</strong>
                            Your stock levels will be reverted and accounting entries
                            reversed.
                        </p>
                        <p v-else class="text-sm text-[var(--ui-text)]">
                            This will cancel the Journal Entry and reverse the
                            accounting entries.
                            <strong>This action cannot be reversed.</strong>
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
                                {{ pendingCancel?.type === 'Expense' ? 'Cancel Expense' : 'Cancel Purchase' }}
                            </UButton>
                        </div>
                    </div>
                </template>
            </UModal>
            <UModal
                v-model:open="openAmendExpense"
                title="Amend Expense"
                :dismissible="false"
            >
                <template #body>
                    <ExpenseForm
                        v-if="amendEntry"
                        :mappings="mappings"
                        :loading="amendExpenseLoading"
                        :amend-entry="amendEntry"
                        @amend="onAmendSubmit"
                        @on-submit="() => {}"
                    />
                </template>
            </UModal>
            <UModal
                v-model:open="openAmendOrder"
                title="Amend Order"
                :dismissible="false"
                :ui="{ content: 'sm:max-w-2xl' }"
            >
                <template #body>
                    <PurchaseForm
                        v-if="amendOrderData"
                        :loading="amendOrderLoading"
                        :amend-order="amendOrderData"
                        @amend="onAmendOrderSubmit"
                        @on-submit="() => {}"
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
            @cancel="onCancelPurchase"
            @amend="onAmendExpense"
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
import PurchaseForm from "@/components/PurchaseForm.vue";
import { ErpNextService } from "@/services/ErpNextService";

const open = ref(false);
const openBulkUpload = ref(false);
const openBulkPreview = ref(false);
const openCancelConfirm = ref(false);
const pendingCancel = ref<Payment | null>(null);
const cancelLoading = ref(false);
const expenseLoading = ref(false);
const bulkLoading = ref(false);

// Amend state
const openAmendExpense = ref(false);
const amendEntry = ref<{
  id: string;
  amount: number;
  description: string;
  expenseTypeId: string;
  date: string;
} | null>(null);
const amendExpenseLoading = ref(false);

const openAmendOrder = ref(false);
const amendOrderData = ref<{
  id: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoiceNumber: string;
  invoiceDate: string;
} | null>(null);
const amendOrderLoading = ref(false);
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
        id,
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

    const isExpense = pendingCancel.value.type === "Expense";
    const result = isExpense
        ? await erpnext.cancelExpenseJournalEntry(pendingCancel.value.id)
        : await erpnext.cancelFullPurchase(pendingCancel.value.id);

    const label = isExpense ? "Expense" : "Purchase";

    if (result) {
        toast.add({
            title: `${label} ${pendingCancel.value.id} cancelled`,
            color: "success",
        });
        await dataStore.update();
    } else {
        toast.add({
            title: `Failed to cancel ${label.toLowerCase()} ${pendingCancel.value.id}`,
            color: "error",
        });
    }
    pendingCancel.value = null;
    cancelLoading.value = false;
    openCancelConfirm.value = false;
}

async function onAmendExpense(payment: Payment) {
  if (payment.type === "Order") {
    const piResponse = await erpnext.getPurchaseInvoice(payment.id);
    if (!piResponse?.data) {
      toast.add({ title: "Failed to fetch purchase invoice for amend", color: "error" });
      return;
    }

    const pi = piResponse.data;
    amendOrderData.value = {
      id: payment.id,
      supplier: pi.supplier,
      warehouse: "",
      items: pi.items.map(item => ({
        item_code: item.item_code,
        qty: item.qty,
        rate: item.rate,
        sell_rate: 0,
      })),
      invoiceNumber: pi.name,
      invoiceDate: pi.posting_date,
    };
    openAmendOrder.value = true;
    return;
  }

  const je = await erpnext.getJournalEntry(payment.id);
  if (!je) {
    toast.add({ title: "Failed to fetch journal entry for amend", color: "error" });
    return;
  }

  const debitAccount = je.accounts?.find(a => a.debit_in_account_currency > 0);
  if (!debitAccount) {
    toast.add({ title: "Could not determine expense account", color: "error" });
    return;
  }

  const mapping = mappings.value.find(
    m => m.erpnextAccountName && debitAccount.account.startsWith(m.erpnextAccountName)
  );
  const expenseTypeId = mapping?.expenseTypeId || "";

  amendEntry.value = {
    id: payment.id,
    amount: payment.amount,
    description: payment.description,
    expenseTypeId,
    date: payment.date,
  };
  openAmendExpense.value = true;
}

async function onAmendSubmit(expense: Expense) {
  amendExpenseLoading.value = true;
  const response = await dataStore.amendDraftExpense(expense);
  amendExpenseLoading.value = false;

  if (response) {
    openAmendExpense.value = false;
    amendEntry.value = null;
    toast.add({
      title: `Expense amended successfully: ${response}`,
      color: "success",
    });
    dataStore.update();
  } else {
    toast.add({
      title: "Failed to amend expense",
      color: "error",
    });
  }
}

async function onAmendOrderSubmit(payload: {
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number: string | null;
  invoice_date: string;
  amended_from: string;
}) {
  amendOrderLoading.value = true;
  const result = await erpnext.amendFullPurchase({
    originalId: payload.amended_from,
    company: authStore.company || "",
    supplier: payload.supplier,
    warehouse: payload.warehouse,
    items: payload.items,
    invoice_number: payload.invoice_number || undefined,
    invoice_date: payload.invoice_date,
  });
  amendOrderLoading.value = false;

  if (result) {
    openAmendOrder.value = false;
    amendOrderData.value = null;
    dataStore.update();
    toast.add({
      title: `Purchase amended: PI ${result.purchase_invoice}`,
      color: "success",
    });
  } else {
    toast.add({
      title: "Failed to amend purchase",
      color: "error",
    });
  }
}
</script>
