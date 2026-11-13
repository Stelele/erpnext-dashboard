<template>
    <div class="p-4 space-y-4">
        <UAlert
            v-if="error"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
        />

        <!-- Step 1: find a paid invoice to refund -->
        <template v-if="!detail">
            <div class="grid grid-cols-2 gap-2">
                <UFormField label="From">
                    <UInput
                        v-model="fromDate"
                        type="date"
                        class="w-full"
                        :disabled="searching || detailLoading"
                    />
                </UFormField>
                <UFormField label="To">
                    <UInput
                        v-model="toDate"
                        type="date"
                        class="w-full"
                        :disabled="searching || detailLoading"
                    />
                </UFormField>
            </div>
            <div class="grid grid-cols-2 gap-2">
                <UFormField label="Status">
                    <USelect
                        v-model="status"
                        :items="statusOptions"
                        placeholder="All statuses"
                        class="w-full"
                        :disabled="searching || detailLoading"
                    />
                </UFormField>
                <UFormField label="Search">
                    <UInput
                        v-model="searchTerm"
                        placeholder="Invoice or customer..."
                        class="w-full"
                        :disabled="searching || detailLoading"
                    />
                </UFormField>
            </div>
            <UButton
                color="primary"
                block
                :loading="searching"
                :disabled="searching || detailLoading"
                @click="searchInvoices"
            >
                Search invoices
            </UButton>

            <div
                v-if="detailLoading"
                class="flex justify-center py-8"
            >
                <UIcon
                    name="i-lucide-loader-2"
                    class="animate-spin text-2xl text-primary"
                />
            </div>

            <div
                v-else-if="searched && !results.length"
                class="py-6 text-center text-sm text-muted"
            >
                No invoices found for these filters.
            </div>

            <div
                v-else-if="results.length"
                class="max-h-72 space-y-2 overflow-y-auto"
            >
                <p
                    v-if="truncated"
                    class="text-xs text-muted"
                >
                    Showing first {{ searchLimit }} invoices — narrow the
                    date range or search to find more.
                </p>
                <button
                    v-for="invoice in results"
                    :key="invoice.name"
                    type="button"
                    class="w-full rounded-lg border border-(--ui-border) p-3 text-left hover:cursor-pointer hover:bg-(--ui-bg-elevated)/60"
                    @click="loadDetail(invoice.name)"
                >
                    <div class="flex items-center justify-between gap-2">
                        <span class="text-sm font-medium">{{
                            invoice.name
                        }}</span>
                        <UBadge
                            class="capitalize"
                            variant="subtle"
                            color="neutral"
                        >
                            {{ invoice.status }}
                        </UBadge>
                    </div>
                    <div class="mt-1 flex items-center justify-between gap-2 text-sm">
                        <span class="truncate text-(--ui-text-muted)">{{
                            invoice.customer_name || invoice.customer
                        }}</span>
                        <span class="font-medium tabular-nums"
                            >{{ formatNumber(invoice.grand_total, "currency") }}
                            {{ invoice.currency }}</span
                        >
                    </div>
                    <div class="text-xs text-(--ui-text-muted)">
                        {{ formatPostingDate(invoice.posting_date, invoice.posting_time) }}
                    </div>
                </button>
            </div>
        </template>

        <!-- Step 2: pick return quantities -->
        <template v-else>
            <div class="rounded-lg bg-(--ui-bg-elevated) p-3 text-sm">
                <div class="flex items-center justify-between gap-2">
                    <span class="font-medium">{{ detail.name }}</span>
                    <UBadge
                        class="capitalize"
                        variant="subtle"
                        color="neutral"
                    >
                        {{ detail.status }}
                    </UBadge>
                </div>
                <div class="mt-1 text-(--ui-text-muted)">
                    {{ detail.customer_name || detail.customer }} &middot;
                    {{ formatNumber(detail.grand_total, "currency") }}
                    {{ detail.currency }}
                </div>
            </div>

            <div class="space-y-2">
                <div
                    v-for="item in detail.items"
                    :key="item.name"
                    class="flex items-center justify-between gap-3 rounded-lg border border-(--ui-border) p-3"
                >
                    <div class="min-w-0 flex-1">
                        <div class="truncate text-sm font-medium">
                            {{ item.item_name }}
                        </div>
                        <div class="text-xs text-(--ui-text-muted)">
                            Returnable: {{ formatNumber(item.returnable_qty, "decimal") }}
                            {{ item.uom }}
                        </div>
                    </div>
                    <UInput
                        v-model="refundQty[item.name]"
                        type="number"
                        :min="0"
                        :max="item.returnable_qty"
                        :step="0.01"
                        class="w-24"
                        :disabled="refunding"
                        @update:model-value="clampQty(item)"
                    />
                </div>
            </div>

            <div class="flex justify-between gap-2 pt-2">
                <UButton
                    color="neutral"
                    variant="outline"
                    :disabled="refunding"
                    @click="detail = null"
                >
                    Back
                </UButton>
                <UButton
                    color="primary"
                    :loading="refunding"
                    :disabled="refunding || !hasSelection"
                    @click="submitRefund"
                >
                    Submit refund
                </UButton>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { ErpNextService } from "@/services/ErpNextService";
import { useDataStore } from "@/stores/DataStore";
import { extractFrappeMessage } from "@/utils/FrappeError";
import { useLatestCall } from "@/composables/useLatestCall";
import type {
    RefundableInvoice,
    RefundInvoiceDetail,
    RefundableItem,
    ReturnItemPayload,
} from "@/types/Refunds";
import { formatNumber } from "@/utils/FormatNumber";
import moment from "moment";

const emit = defineEmits<{
    success: [returnInvoice: string];
}>();

const erpnext = new ErpNextService();
const dataStore = useDataStore();

const statusOptions = ["Paid", "Unpaid", "Overdue", "Cancelled"];

const fromDate = ref(dataStore.dateRange.start);
const toDate = ref(dataStore.dateRange.end);
const status = ref("");
const searchTerm = ref("");

const searching = ref(false);
const searched = ref(false);
const results = ref<RefundableInvoice[]>([]);
const searchLimit = 50;
const truncated = ref(false);
const searchGuard = useLatestCall();

const detailLoading = ref(false);
const detail = ref<RefundInvoiceDetail | null>(null);
const refundQty = ref<Record<string, number>>({});
const detailGuard = useLatestCall();

const refunding = ref(false);
const error = ref<string | null>(null);

const hasSelection = computed(() =>
    (detail.value?.items ?? []).some((item) => (refundQty.value[item.name] ?? 0) > 0),
);

function formatPostingDate(date: string, time: string): string {
    return moment(`${date} ${time}`).format("DD MMM YYYY HH:mm");
}

async function searchInvoices() {
    const seq = searchGuard.start();
    searching.value = true;
    error.value = null;
    truncated.value = false;
    try {
        const invoices = await erpnext.getPosInvoicesForRefund({
            from_date: fromDate.value || undefined,
            to_date: toDate.value || undefined,
            status: status.value || undefined,
            search_term: searchTerm.value || undefined,
            limit: searchLimit + 1,
        });
        if (!searchGuard.isCurrent(seq)) return;
        const list = invoices ?? [];
        truncated.value = list.length > searchLimit;
        results.value = list.slice(0, searchLimit);
        searched.value = true;
    } catch (err) {
        if (!searchGuard.isCurrent(seq)) return;
        error.value = extractFrappeMessage(err);
    } finally {
        if (searchGuard.isCurrent(seq)) searching.value = false;
    }
}

async function loadDetail(invoiceName: string) {
    const seq = detailGuard.start();
    detailLoading.value = true;
    error.value = null;
    try {
        const invoice = await erpnext.getInvoiceDetailForRefund(invoiceName);
        if (!detailGuard.isCurrent(seq)) return;
        if (!invoice) {
            error.value = "Failed to load invoice details.";
            return;
        }
        detail.value = invoice;
        const qty: Record<string, number> = {};
        for (const item of invoice.items) {
            qty[item.name] = 0;
        }
        refundQty.value = qty;
    } catch (err) {
        if (!detailGuard.isCurrent(seq)) return;
        error.value = extractFrappeMessage(err);
    } finally {
        if (detailGuard.isCurrent(seq)) detailLoading.value = false;
    }
}

function sanitizedQty(item: RefundableItem, value: unknown): number {
    const raw = Number(value ?? 0);
    const safe = Number.isFinite(raw) ? raw : 0;
    return Math.min(Math.max(0, safe), item.returnable_qty);
}

function clampQty(item: RefundableItem) {
    refundQty.value[item.name] = sanitizedQty(item, refundQty.value[item.name]);
}

async function submitRefund() {
    if (!detail.value || refunding.value) return;
    const invoiceName = detail.value.name;
    const selected: ReturnItemPayload[] = [];
    for (const item of detail.value.items) {
        const qty = sanitizedQty(item, refundQty.value[item.name]);
        if (qty > 0) {
            selected.push({ item_name: item.name, qty });
        }
    }
    if (!selected.length) {
        error.value = "Select a quantity greater than zero for at least one item.";
        return;
    }
    refunding.value = true;
    error.value = null;
    try {
        const result = await erpnext.processPosRefund(invoiceName, selected);
        detail.value = null;
        refundQty.value = {};
        emit("success", result.return_invoice);
    } catch (err) {
        error.value = extractFrappeMessage(err);
    } finally {
        refunding.value = false;
    }
}

void searchInvoices();
</script>
