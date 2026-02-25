<template>
    <UPageCard
        class="min-h-96 max-h-[86vh] col-span-6"
        title="Current Stock Levels"
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
                :ui="{ tr: 'data-[expanded=true]:bg-elevated/50' }"
                class="flex-1 h-full"
                loadingColor="primary"
            >
                <template #expanded="{ row }">
                    <div class="grid grid-cols-2 w-full md:w-1/2 px-1 md:px-4">
                        <div>Item</div>
                        <div>{{ row.original.item_name }}</div>
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
                </template>
            </UTable>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import type { TableColumn } from "@nuxt/ui";
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
const UButton = resolveComponent("UButton");
const expanded = ref({});

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
];
</script>
