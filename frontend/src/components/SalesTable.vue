<template>
    <UPageCard
        class="min-h-96 col-span-6"
        title="Sales"
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
                :data="groupedSalesDetails"
                :columns="columns"
                :grouping="['posting_date', 'item_group']"
                :grouping-options="grouping_options"
                :ui="{
                    root: 'min-w-full',
                    td: 'empty:p-0', // helps with the colspaned row added for expand slot
                }"
            >
                <template #title-cell="{ row }">
                    <div v-if="row.getIsGrouped()" class="flex items-center">
                        <span
                            class="inline-block"
                            :style="{ width: `calc(${row.depth} * 1rem)` }"
                        />

                        <UButton
                            variant="outline"
                            color="neutral"
                            class="mr-2"
                            size="xs"
                            :icon="
                                row.getIsExpanded()
                                    ? 'i-lucide-minus'
                                    : 'i-lucide-plus'
                            "
                            @click="row.toggleExpanded()"
                        />
                        <strong
                            v-if="row.groupingColumnId === 'posting_date'"
                            >{{ row.original.posting_date }}</strong
                        >
                        <strong
                            v-else-if="row.groupingColumnId === 'item_group'"
                            >{{ row.original.item_group }}</strong
                        >
                    </div>
                </template>
            </UTable>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { TableColumn } from "@nuxt/ui";
import { getGroupedRowModel } from "@tanstack/vue-table";
import type { GroupingOptions } from "@tanstack/vue-table";
import type { SalesDetail } from "@/types/SalesDetail";
import moment from "moment";

export interface Props {
    data: SalesDetail[];
}

const props = defineProps<Props>();

const groupedSalesDetails = computed(() => {
    const temp: SalesDetail[] = [];
    for (const saleDetail of props.data) {
        const entry = temp.find((item) => {
            const itemDate = moment(item.posting_date).format("DD MMM YYYY");
            const saleDate = moment(saleDetail.posting_date).format(
                "DD MMM YYYY",
            );
            return (
                itemDate === saleDate &&
                item.item_name === saleDetail.item_name &&
                item.item_group === saleDetail.item_group
            );
        });

        if (!entry) {
            temp.push(saleDetail);
        } else {
            entry.qty += saleDetail.qty;
            entry.item_total_amount += saleDetail.item_total_amount;
        }
    }

    return temp;
});

const columns: TableColumn<SalesDetail>[] = [
    {
        id: "title",
        header: "Date",
    },
    {
        id: "posting_date",
        accessorKey: "posting_date",
        header: "Date",
        cell: ({ row }) => {
            return moment(row.getValue("posting_date")).format("DD MMM YYYY");
        },
        aggregationFn: "max",
    },
    {
        id: "item_name",
        header: "Name",
        accessorKey: "item_name",
    },
    {
        id: "item_group",
        header: "Category",
        accessorKey: "item_group",
        aggregationFn: "count",
    },
    {
        id: "qty",
        accessorKey: "qty",
        header: "Quantity",
        cell: ({ row }) => {
            const qty = Number.parseFloat(row.getValue("qty"));
            return new Intl.NumberFormat("en-ZW", {
                style: "decimal",
                maximumFractionDigits: 2,
            }).format(qty);
        },
        aggregationFn: "sum",
    },
    {
        id: "rate",
        header: "Unit Price",
        accessorKey: "rate",
    },
    {
        id: "item_total_amount",
        header: "Amount",
        accessorKey: "item_total_amount",
        meta: {
            class: {
                th: "text-right",
                td: "text-right font-medium",
            },
        },
        cell: ({ row }) => {
            const amount = Number.parseFloat(row.getValue("item_total_amount"));
            return new Intl.NumberFormat("en-ZW", {
                style: "currency",
                currency: "USD",
            }).format(amount);
        },
        aggregationFn: "sum",
    },
];

const grouping_options = ref<GroupingOptions>({
    groupedColumnMode: "remove",
    getGroupedRowModel: getGroupedRowModel(),
});
</script>
