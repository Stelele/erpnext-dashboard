<template>
    <UPageCard
        class="min-h-96 max-h-[86vh] col-span-6"
        title="Expenses"
        :ui="{
            container: 'gap-y-1.5',
            wrapper: 'items-start',
            leading:
                'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
            title: 'font-bold text-xs uppercase',
        }"
    >
        <div class="overflow-x-auto">
            <UTable
                v-model:expanded="expanded"
                :sticky="true"
                :data="props.data"
                :columns="columns"
                :loading="props.loading"
                :get-row-id="(row: any) => row.id"
                :ui="{ tr: 'data-[expanded=true]:bg-elevated/50' }"
                class="flex-1 h-full"
                loadingColor="primary"
                @select="onRowSelect"
            >
                <template #expanded="{ row }">
                    <div class="mx-2 mb-2 rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated)/60 p-3 shadow-sm">
                        <!-- Section 1: Summary -->
                        <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-sm items-start">
                            <div class="text-(--ui-text-muted)">Date</div>
                            <div>{{ moment(row.original.date).format("DD MMM YYYY") }}</div>
                            <div class="text-(--ui-text-muted)">#</div>
                            <div class="truncate">{{ row.original.id }}</div>
                            <div class="text-(--ui-text-muted)">Status</div>
                            <div>
                                <UBadge class="capitalize" variant="subtle" :color="getStatusColor(row.original.status)">
                                    {{ row.original.status }}
                                </UBadge>
                            </div>
                            <div class="text-(--ui-text-muted)">Type</div>
                            <div>{{ row.original.type }}</div>
                            <div class="text-(--ui-text-muted)">
                                {{ row.original.type === 'Order' ? 'Supplier' : 'Description' }}
                            </div>
                            <div class="text-wrap break-words">
                                {{ row.original.type === 'Order' ? (invoiceFor(row.original.id)?.supplier ?? '—') : row.original.description }}
                            </div>
                            <div class="text-(--ui-text-muted)">Amount</div>
                            <div class="font-medium">{{ formatNumber(row.original.amount, "currency") }}</div>
                        </div>

                        <!-- Section 2: Line Items (Order type only) -->
                        <template v-if="row.original.type === 'Order'">
                            <div class="my-3 border-t border-(--ui-border)"></div>
                            <div class="flex items-center gap-2 mb-2">
                                <span class="text-xs text-(--ui-text-muted) uppercase tracking-wider">Line Items</span>
                                <span class="text-xs text-(--ui-text-muted)">
                                    &middot; {{ invoiceFor(row.original.id)?.items?.length ?? 0 }}
                                </span>
                            </div>

                            <!-- Loading skeleton -->
                            <div v-if="isInvoiceLoading(row.original.id)" class="animate-pulse space-y-2 py-1">
                                <div class="h-3 bg-(--ui-border) rounded w-full"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-3/4"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-5/6"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-1/2"></div>
                            </div>

                            <!-- Error state -->
                            <div v-else-if="hasInvoiceError(row.original.id)" class="flex items-center gap-2 py-1 text-sm text-(--ui-error)">
                                <span class="i-lucide-alert-circle size-4 inline-block"></span>
                                <span>Failed to load line items</span>
                                <UButton size="xs" variant="ghost" color="error" @click="fetchInvoice(row.original.id)">
                                    Retry
                                </UButton>
                            </div>

                            <!-- Empty items -->
                            <div v-else-if="!invoiceFor(row.original.id)?.items?.length" class="py-1 text-xs text-(--ui-text-muted)">
                                No line items
                            </div>

                            <!-- Line items table -->
                            <div v-else role="grid" tabindex="0" class="border border-(--ui-border) rounded-md overflow-hidden max-h-36 overflow-y-auto">
                                <div class="grid grid-cols-[1fr_auto_auto] gap-x-3">
                                    <div role="columnheader" class="px-3 py-1.5 bg-(--ui-bg-elevated) text-xs text-(--ui-text-muted) uppercase sticky top-0">Item</div>
                                    <div role="columnheader" class="px-3 py-1.5 bg-(--ui-bg-elevated) text-xs text-(--ui-text-muted) uppercase sticky top-0 text-right">Qty</div>
                                    <div role="columnheader" class="px-3 py-1.5 bg-(--ui-bg-elevated) text-xs text-(--ui-text-muted) uppercase sticky top-0 text-right">Total</div>
                                    <template v-for="item in invoiceFor(row.original.id)!.items" :key="item.item_code">
                                        <div role="gridcell" class="px-3 py-1.5 text-sm border-t border-(--ui-border) truncate">{{ item.item_name }}</div>
                                        <div role="gridcell" class="px-3 py-1.5 text-sm border-t border-(--ui-border) text-right tabular-nums">{{ item.qty }}</div>
                                        <div role="gridcell" class="px-3 py-1.5 text-sm border-t border-(--ui-border) text-right tabular-nums font-medium">{{ formatNumber(item.amount, "currency") }}</div>
                                    </template>
                                </div>
                            </div>
                        </template>

                        <!-- Section 3: Actions -->
                        <div
                            v-if="row.original.status === 'Submitted'"
                            class="mt-3 pt-3 border-t border-(--ui-border) flex gap-2"
                        >
                            <UButton
                                color="error"
                                variant="ghost"
                                icon="i-lucide-x"
                                @click="emit('cancel', row.original)"
                            >
                                Cancel
                            </UButton>
                            <UButton
                                color="success"
                                variant="ghost"
                                icon="i-lucide-pencil"
                                @click="emit('amend', row.original)"
                            >
                                Amend
                            </UButton>
                        </div>
                    </div>
                </template>
            </UTable>

        </div>
            <OrderDetailsModal
                :purchase-invoice-id="selectedOrderId"
                @close="selectedOrderId = null"
            />
    </UPageCard>
</template>

<script setup lang="ts">
import { h, ref, resolveComponent, watch } from "vue";
import type { TableColumn, TableRow } from "@nuxt/ui";
import type { Payment } from "@/types/Expenses";
import moment from "moment";
import { formatNumber } from "@/utils/FormatNumber";
import { ErpNextService } from "@/services/ErpNextService";
import type { PurchaseInvoiceResponse } from "@/types/Expenses";
import OrderDetailsModal from "@/components/OrderDetailsModal.vue";

const UBadge = resolveComponent("UBadge");
const UButton = resolveComponent("UButton");

const props = defineProps<{
    data: Payment[];
    loading: boolean;
}>();

const emit = defineEmits<{
    cancel: [payment: Payment];
    amend: [payment: Payment];
}>();

const erpnext = new ErpNextService();

const purchaseInvoices = ref<Map<string, PurchaseInvoiceResponse["data"]>>(new Map());
const invoiceLoading = ref<Set<string>>(new Set());
const invoiceError = ref<Set<string>>(new Set());

watch(() => props.data, () => {
    purchaseInvoices.value.clear();
    invoiceLoading.value.clear();
    invoiceError.value.clear();
});

const expanded = ref<Record<string, boolean>>({});
const selectedOrderId = ref<string | null>(null);

watch(expanded, (newVal: Record<string, boolean>, oldVal: Record<string, boolean>) => {
  const oldExpanded = new Set(oldVal ? Object.keys(oldVal).filter((k) => oldVal[k]) : []);
  const newExpanded = Object.keys(newVal).filter((k) => newVal[k]);
  for (const key of newExpanded) {
    if (!oldExpanded.has(key)) {
      const payment = props.data.find((p) => p.id === key);
      if (payment?.type === "Order") {
        ensureInvoice(key);
      }
    }
  }
}, { deep: true });

function ensureInvoice(id: string) {
    if (purchaseInvoices.value.has(id) || invoiceLoading.value.has(id)) return;
    fetchInvoice(id);
}

function fetchInvoice(id: string) {
    invoiceError.value.delete(id);
    if (invoiceLoading.value.has(id)) return;
    invoiceLoading.value.add(id);
    erpnext.getPurchaseInvoice(id).then((resp) => {
        invoiceLoading.value.delete(id);
        if (resp?.data) {
            invoiceError.value.delete(id);
            purchaseInvoices.value.set(id, resp.data);
        } else {
            invoiceError.value.add(id);
        }
    }).catch(() => {
        invoiceLoading.value.delete(id);
        invoiceError.value.add(id);
    });
}

function invoiceFor(id: string): PurchaseInvoiceResponse["data"] | undefined {
    return purchaseInvoices.value.get(id);
}

function isInvoiceLoading(id: string): boolean {
    return invoiceLoading.value.has(id);
}

function hasInvoiceError(id: string): boolean {
    return invoiceError.value.has(id);
}

function onRowSelect(e: Event, row: TableRow<Payment>) {
    if (window.matchMedia("(min-width: 768px)").matches) return;
    const target = e.target as HTMLElement;
    if (target.closest("button")) return;
    row.toggleExpanded();
}

const columns: TableColumn<Payment>[] = [
    {
        id: "expand",
        meta: {
            class: {
                th: "table-cell md:hidden",
                td: "table-cell md:hidden",
            },
        },
        cell: ({ row }) =>
            h(UButton, {
                color: "neutral",
                variant: "ghost",
                icon: "i-lucide-chevron-down",
                square: true,
                "aria-label": "Expand",
                ui: {
                    leadingIcon: [
                        "transition-transform",
                        row.getIsExpanded() ? "duration-200 rotate-180" : "",
                    ],
                },
                onClick: () => row.toggleExpanded(),
            }),
    },
    {
        accessorKey: "id",
        header: "#",
        meta: {
            class: {
                th: "hidden xl:table-cell",
                td: "hidden xl:table-cell",
            },
        },
        cell: ({ row }) => `${row.getValue("id")}`,
    },
    {
        accessorKey: "date",
        header: "Date",
        cell: ({ row }) => {
            return moment(row.getValue("date")).format("DD MMM YYYY");
        },
    },
    {
        accessorKey: "status",
        header: "Status",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "hidden md:table-cell",
            },
        },
        cell: ({ row }) => getStatusElement(row.original.status),
    },
    {
        accessorKey: "type",
        header: "Type",
    },
    {
        accessorKey: "description",
        header: "Description",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "max-w-[100px] md:max-w-[200px] lg:max-w-[300px] overflow-hidden text-ellipsis whitespace-nowrap hidden md:table-cell",
            },
        },
    },
    {
        accessorKey: "amount",
        header: "Amount",
        meta: {
            class: {
                th: "text-right",
                td: "text-right font-medium",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("amount"));
            return formatNumber(amount, "currency");
        },
    },
    {
        id: "actions",
        header: "",
        meta: {
            class: {
                th: "w-0 hidden md:table-cell",
                td: "w-0 hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            const buttons = [];

            if (row.original.type === "Order") {
                buttons.push(
                    h(UButton, {
                        color: "neutral",
                        variant: "ghost",
                        icon: "i-lucide-info",
                        square: true,
                        "aria-label": "Order details",
                        onClick: () => {
                            selectedOrderId.value = row.original.id;
                        },
                    })
                );
            }

            if (row.original.status === "Submitted") {
                const cancelLabel = row.original.type === "Order" ? "Cancel purchase" : "Cancel expense";
                buttons.push(
                    h(UButton, {
                        color: "error",
                        variant: "ghost",
                        icon: "i-lucide-x",
                        square: true,
                        "aria-label": cancelLabel,
                        onClick: () => emit("cancel", row.original),
                    })
                );
                buttons.push(
                    h(UButton, {
                        color: "success",
                        variant: "ghost",
                        icon: "i-lucide-pencil",
                        square: true,
                        "aria-label": "Amend",
                        onClick: () => emit("amend", row.original),
                    })
                );
            }

            if (buttons.length === 0) return "";
            return h("div", { style: { display: "flex", gap: "4px" } }, buttons);
        },
    },
];

function getStatusElement(status: Payment["status"]) {
    const color = getStatusColor(status);

    return h(
        UBadge,
        { class: "capitalize", variant: "subtle", color },
        () => status,
    );
}

function getStatusColor(status: Payment["status"]) {
    return {
        Submitted: "success" as const,
        Cancelled: "error" as const,
        Draft: "neutral" as const,
    }[status] ?? "neutral" as const;
}
</script>
