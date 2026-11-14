<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                v-model:open="openNewRefund"
                title="New Refund"
                :dismissible="false"
                :ui="{ content: 'sm:max-w-2xl' }"
            >
                <UButton
                    trailing-icon="i-lucide-undo-2"
                    class="hover:cursor-pointer"
                    >New Refund</UButton
                >
                <template #body>
                    <NewRefundModal @success="onRefundSuccess" />
                </template>
            </UModal>
        </div>
        <UPageCard
            class="min-h-96 col-span-6"
            title="Refunds"
            :ui="{
                container: 'gap-y-1.5',
                wrapper: 'flex-initial items-start',
                leading:
                    'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
                title: 'font-bold text-xs uppercase',
            }"
        >
            <UAlert
                v-if="error"
                color="error"
                variant="subtle"
                icon="i-lucide-circle-alert"
                :title="error"
            />
            <template v-if="loading || refunds.length">
                <div class="hidden md:block">
                    <UTable
                        :data="refunds"
                        :columns="columns"
                        :loading="loading"
                        :sticky="true"
                        :ui="{ root: 'max-h-[86vh] overflow-auto' }"
                        loadingColor="primary"
                        class="flex-1"
                    />
                </div>
                <div class="md:hidden max-h-[86vh] overflow-auto">
                    <UCard
                        v-for="r in refunds"
                        :key="r.name"
                        class="mt-2"
                    >
                        <div class="flex justify-between items-start">
                            <div class="flex flex-col">
                                <div class="font-semibold">
                                    {{ r.name }}
                                </div>
                                <div class="text-sm text-muted">
                                    Return against: {{ r.return_against ?? "—" }}
                                </div>
                                <div class="text-sm text-muted">
                                    {{ r.customer_name || r.customer }}
                                </div>
                            </div>
                            <div class="font-bold">
                                {{
                                    formatNumber(
                                        r.grand_total,
                                        "currency",
                                    )
                                }}
                            </div>
                        </div>
                        <div class="text-xs text-muted mt-1">
                            {{ formatPosted(r) }}
                        </div>
                    </UCard>
                </div>
            </template>
            <p
                v-else
                class="py-8 text-center text-sm text-muted"
            >
                No refunds found for this period.
            </p>
        </UPageCard>
    </DashboardLayout>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import type { TableColumn } from "@nuxt/ui";
import { useToast } from "@nuxt/ui/composables";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import NewRefundModal from "@/components/NewRefundModal.vue";
import { useDataStore } from "@/stores/DataStore";
import { useAuthStore } from "@/stores/AuthStore";
import { ErpNextService } from "@/services/ErpNextService";
import { extractFrappeMessage } from "@/utils/FrappeError";
import { useLatestCall } from "@/composables/useLatestCall";
import type { RefundHistoryItem } from "@/types/Refunds";
import { formatNumber } from "@/utils/FormatNumber";
import moment from "moment";

const dataStore = useDataStore();
const authStore = useAuthStore();
const toast = useToast();
const erpnext = new ErpNextService();

const openNewRefund = ref(false);
const loading = ref(false);
const error = ref<string | null>(null);
const refunds = ref<RefundHistoryItem[]>([]);
const historyGuard = useLatestCall();

async function loadHistory() {
    if (!authStore.url) return;
    const seq = historyGuard.start();
    loading.value = true;
    error.value = null;
    try {
        const items = await erpnext.getRefundsForPeriod(
            dataStore.dateRange.start,
            dataStore.dateRange.end,
        );
        if (!historyGuard.isCurrent(seq)) return;
        refunds.value = items ?? [];
    } catch (err) {
        if (!historyGuard.isCurrent(seq)) return;
        error.value = extractFrappeMessage(err);
    } finally {
        if (historyGuard.isCurrent(seq)) loading.value = false;
    }
}

function formatPosted(r: RefundHistoryItem): string {
    return moment(`${r.posting_date} ${r.posting_time}`).format(
        "DD MMM YYYY HH:mm",
    );
}

function onRefundSuccess(returnInvoice: string) {
    openNewRefund.value = false;
    toast.add({
        title: `Refund created: ${returnInvoice}`,
        color: "success",
    });
    void loadHistory();
}

onMounted(() => {
    void loadHistory();
});

watch(
    [() => authStore.url, () => dataStore.dateRange],
    () => {
        if (authStore.url) {
            void loadHistory();
        }
    },
);

const columns: TableColumn<RefundHistoryItem>[] = [
    {
        accessorKey: "name",
        header: "Invoice",
    },
    {
        accessorKey: "return_against",
        header: "Return Against",
        cell: ({ row }) => row.getValue("return_against") ?? "—",
    },
    {
        accessorKey: "customer_name",
        header: "Customer",
        cell: ({ row }) => row.original.customer_name || row.original.customer,
    },
    {
        accessorKey: "grand_total",
        header: "Total",
        meta: {
            class: {
                td: "text-right font-medium tabular-nums",
            },
        },
        cell: ({ row }) =>
            `${formatNumber(row.original.grand_total, "currency")} ${row.original.currency}`,
    },
    {
        accessorKey: "posting_date",
        header: "Posted",
        cell: ({ row }) =>
            moment(`${row.original.posting_date} ${row.original.posting_time}`).format(
                "DD MMM YYYY HH:mm",
            ),
    },
];
</script>
