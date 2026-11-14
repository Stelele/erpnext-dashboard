<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                :open="openNewRefund"
                title="New Refund"
                :dismissible="false"
                :ui="{ content: 'sm:max-w-2xl' }"
                @update:open="(v) => {
                    if (!v && refundInFlight) return;
                    openNewRefund = v;
                }"
            >
                <UButton
                    trailing-icon="i-lucide-undo-2"
                    class="hover:cursor-pointer"
                    >New Refund</UButton
                >
                <template #body>
                    <NewRefundModal
                        @success="onRefundSuccess"
                        @update:busy="refundInFlight = $event"
                    />
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
                        @select="onRefundSelect"
                    />
                </div>
                <div class="md:hidden max-h-[86vh] overflow-auto">
                    <USkeleton
                        v-if="loading && !refunds.length"
                        class="h-24 mt-2"
                    />
                    <UCard
                        v-for="r in refunds"
                        :key="r.name"
                        class="mt-2 hover:cursor-pointer"
                        @click="selectedRefund = r"
                    >
                        <div class="flex justify-between items-start gap-2">
                            <div class="font-semibold min-w-0 break-words">
                                {{ itemSummary(r) }}
                            </div>
                            <div class="font-bold shrink-0 tabular-nums">
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
        <RefundDetailModal
            :refund="selectedRefund"
            @close="selectedRefund = null"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import type { TableColumn, TableRow } from "@nuxt/ui";
import { useToast } from "@nuxt/ui/composables";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import NewRefundModal from "@/components/NewRefundModal.vue";
import RefundDetailModal from "@/components/RefundDetailModal.vue";
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
const refundInFlight = ref(false);
const selectedRefund = ref<RefundHistoryItem | null>(null);
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

function itemSummary(r: RefundHistoryItem): string {
    if (!r.items?.length) return "—";
    return r.items
        .map((item) => `×${item.qty} ${item.item_name}`)
        .join(", ");
}

function onRefundSelect(_e: Event, row: TableRow<RefundHistoryItem>) {
    selectedRefund.value = row.original;
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
        id: "items",
        header: "Items",
        meta: {
            class: {
                td: "font-medium",
            },
        },
        cell: ({ row }) => itemSummary(row.original),
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
