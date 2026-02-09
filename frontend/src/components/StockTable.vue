<template>
    <UPageCard
        class="min-h-96 col-span-6"
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
                :data="processedData"
                :columns="columns"
                :loading="props.loading"
                loadingColor="primary"
            />
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import type { TableColumn } from "@nuxt/ui";
import type { StockDetail } from "@/types/StockDetail";
import { computed } from "vue";

export interface Props {
    data: StockDetail[];
    loading: boolean;
}

const props = defineProps<Props>();

const processedData = computed(() => {
    return props.data.sort((a, b) => {
        if (a.item_group.localeCompare(b.item_group) === 0) {
            return a.item_name.localeCompare(b.item_name);
        }

        return a.item_group.localeCompare(b.item_group);
    });
});

const columns: TableColumn<StockDetail>[] = [
    {
        id: "item_group",
        header: "Group",
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
            return new Intl.NumberFormat("en-ZW", {
                style: "decimal",
                maximumFractionDigits: 3,
            }).format(row.original.real_qty);
        },
    },
    {
        accessorKey: "buying_price",
        header: "Order Price",
        meta: {
            class: {
                th: "text-right",
                td: "text-right font-medium",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("buying_price"));
            if (isNaN(amount)) return "-";
            return new Intl.NumberFormat("en-US", {
                style: "currency",
                currency: "USD",
            }).format(amount);
        },
    },
    {
        accessorKey: "selling_price",
        header: "Selling Price",
        meta: {
            class: {
                th: "text-right",
                td: "text-right font-medium",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("selling_price"));
            return new Intl.NumberFormat("en-US", {
                style: "currency",
                currency: "USD",
            }).format(amount);
        },
    },
];
</script>
