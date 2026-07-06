<template>
    <UPageCard
        class="min-h-96 col-span-6"
        title="Current Stock Levels"
        :ui="{
            container: 'gap-y-1.5',
            wrapper: 'flex-initial items-start',
            leading:
                'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
            title: 'font-bold text-xs uppercase',
        }"
    >
        <UTable
            v-model:expanded="expanded"
            :sticky="true"
            :virtualize="true"
            :data="props.data"
            :columns="columns"
            :loading="props.loading"
            :get-row-id="(row: StockRow) => row.item_code"
            :ui="{ tr: 'data-[expanded=true]:bg-elevated/50', root: 'max-h-[86vh] overflow-auto' }"
            class="flex-1"
            loadingColor="primary"
            @select="onRowSelect"
        >
            <template #expanded="{ row }">
                <div
                    class="grid grid-cols-2 w-full md:w-1/2 px-1 md:px-4 text-wrap"
                >
                    <div>Item</div>
                    <div>
                        {{ row.original.item_name }}
                    </div>
                    <div>Group</div>
                    <div>{{ row.original.item_group }}</div>
                    <div>Current Quantity</div>
                    <div>
                        {{ formatNumber(row.original.real_qty, "decimal") }}
                    </div>
                    <div v-if="row.original.pack_size">Pack Size</div>
                    <div v-if="row.original.pack_size">
                        {{ row.original.pack_size }}
                    </div>
                    <div>Unit Order Price</div>
                    <div>
                        {{
                            formatNumber(
                                row.original.buying_price,
                                "currency",
                            )
                        }}
                    </div>
                    <div>Unit Selling Price</div>
                    <div>
                        {{
                            formatNumber(
                                row.original.selling_price,
                                "currency",
                            )
                        }}
                    </div>
                    <div>Unit Gross Profit</div>
                    <div>
                        {{
                            formatNumber(
                                row.original.gross_profit,
                                "currency",
                            )
                        }}
                    </div>
                    <div>Total Gross Profit</div>
                    <div>
                        {{
                            formatNumber(
                                row.original.total_gross_profit,
                                "currency",
                            )
                        }}
                    </div>
                </div>
                    <div class="w-full pt-3 mt-3 border-t border-[var(--ui-border)]">
                        <UButton
                            color="error"
                            variant="outline"
                            icon="i-lucide-ban"
                            size="sm"
                            @click="emit('disableItem', row.original)"
                        >
                            Disable Item
                        </UButton>
                    </div>
            </template>
        </UTable>
    </UPageCard>
</template>

<script setup lang="ts">
import type { TableColumn, TableRow } from "@nuxt/ui";
import type { StockDetail } from "@/types/StockDetail";
import { h, ref, resolveComponent } from "vue";
import { formatNumber } from "@/utils/FormatNumber";

export interface Props {
    data: StockRow[];
    loading: boolean;
}

export type StockRow = StockDetail & {
    gross_profit: number;
    total_gross_profit: number;
    packageSize?: string;
};

const props = defineProps<Props>();
const emit = defineEmits<{
  disableItem: [row: StockRow];
}>();
const UButton = resolveComponent("UButton");
const expanded = ref({});

function onRowSelect(e: Event, row: TableRow<StockRow>) {
    if (window.matchMedia("(min-width: 768px)").matches) return;
    row.toggleExpanded();
}

const columns: TableColumn<StockRow>[] = [
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
        id: "item_group",
        header: "Group",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            return row.original.item_group;
        },
    },
    {
        id: "item_name",
        header: "Item",
        meta: {
            class: {
                td: "max-w-[160px] md:max-w-[200px] lg:max-w-[300px] overflow-hidden text-ellipsis",
            },
        },
        cell: ({ row }) => {
            return row.original.item_name;
        },
    },
    {
        id: "real_qty",
        header: "Current Quantity",
        cell: ({ row }) => {
            if (row.original.pack_size) {
                return row.original.pack_size;
            }

            return formatNumber(row.original.real_qty, "decimal");
        },
    },
    {
        accessorKey: "buying_price",
        header: "Order Price",
        meta: {
            class: {
                th: "text-right hidden md:table-cell",
                td: "text-right font-medium hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("buying_price"));
            if (isNaN(amount)) return "-";
            return formatNumber(amount, "currency");
        },
    },
    {
        accessorKey: "selling_price",
        header: "Selling Price",
        meta: {
            class: {
                th: "text-right hidden md:table-cell",
                td: "text-right font-medium hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("selling_price"));
            return formatNumber(amount, "currency");
        },
    },
    {
        accessorKey: "gross_profit",
        header: "Unit Gross Profit",
        meta: {
            class: {
                th: "text-righ hidden md:table-cell",
                td: "text-right font-medium hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("gross_profit"));
            return formatNumber(amount, "currency");
        },
    },
    {
        accessorKey: "total_gross_profit",
        header: "Total Gross Profit",
        meta: {
            class: {
                th: "text-right hidden md:table-cell",
                td: "text-right font-medium hidden md:table-cell",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(
                row.getValue("total_gross_profit"),
            );
            return formatNumber(amount, "currency");
        },
    },
    {
        id: "actions",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "hidden md:table-cell",
            },
        },
        cell: ({ row }) =>
            h(UButton, {
                color: "error",
                variant: "ghost",
                icon: "i-lucide-ban",
                square: true,
                onClick: () => emit("disableItem", row.original),
            }),
    },
];
</script>
