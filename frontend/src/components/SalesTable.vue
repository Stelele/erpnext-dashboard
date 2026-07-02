<template>
    <UPageCard
        class="min-h-96 col-span-6"
        title="Sales"
        :ui="{
            container: 'gap-y-1.5',
            wrapper: 'flex-initial items-start',
            leading:
                'p-2.5 rounded-full bg-primary/10 ring ring-inset ring-primary/25 flex-col',
            title: 'font-bold text-xl md:text-xs uppercase',
        }"
    >
        <div class="overflow-x-auto">
            <div class="hidden md:block">
                <UTable
                    :data="props.salesDetails"
                    :columns="columns"
                    :grouping="['posting_date', 'item_group']"
                    :grouping-options="grouping_options"
                    :loading="props.loading"
                    :sticky="true"
                    :virtualize="true"
                    :ui="{
                        root: 'min-w-full max-h-[86vh] overflow-auto',
                        td: 'empty:p-0',
                    }"
                >
                    <template #title-cell="{ row }">
                        <div
                            v-if="row.getIsGrouped()"
                            class="flex items-center"
                        >
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
                                v-else-if="
                                    row.groupingColumnId === 'item_group'
                                "
                                >{{ row.original.item_group }}</strong
                            >
                        </div>
                    </template>
                </UTable>
            </div>
            <div ref="mobileWrapperRef" class="md:hidden overflow-auto max-h-[86vh]">
                <div :style="{ height: mobileTopSpacer + 'px' }"></div>
                <div v-for="row in mobileVisibleRows" :key="mobileRowKey(row)">
                    <template v-if="row.type === 'date'">
                        <h3 class="font-bold text-lg p-2">
                            {{ row.date }}
                        </h3>
                    </template>
                    <UCard
                        v-else
                        class="mt-2"
                    >
                        <div class="flex justify-between items-start">
                            <div class="flex flex-col">
                                <div class="font-semibold">
                                    {{ row.item.item_name }}
                                </div>
                                <div class="flex gap-1">
                                    <UBadge
                                        color="primary"
                                        variant="subtle"
                                        size="sm"
                                    >
                                        {{ row.item.item_group }}
                                    </UBadge>
                                    <div class="font-light italic text-sm">
                                        {{ formatNumber(row.item.qty, "decimal") }}
                                        @
                                        {{
                                            formatNumber(row.item.rate, "currency")
                                        }}
                                    </div>
                                </div>
                            </div>
                            <div class="font-bold">
                                {{
                                    formatNumber(
                                        row.item.rate * row.item.qty,
                                        "currency",
                                    )
                                }}
                            </div>
                        </div>
                    </UCard>
                </div>
                <div :style="{ height: mobileBottomSpacer + 'px' }"></div>
            </div>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { ref, computed } from "vue";
import type { TableColumn } from "@nuxt/ui";
import { getGroupedRowModel } from "@tanstack/vue-table";
import type { GroupingOptions } from "@tanstack/vue-table";
import type { SalesDetail } from "@/types/SalesDetail";
import moment from "moment";
import { formatNumber } from "@/utils/FormatNumber";
import { useVisibleWindow } from "@/composables/useVisibleWindow";

export interface Props {
    loading: boolean;
    salesDetails: SalesDetail[];
    mobileSalesDetails: Record<string, SalesDetail[]>;
    mobileSalesDateDetails: string[];
}

const props = defineProps<Props>();

const grouping_options = ref<GroupingOptions>({
    groupedColumnMode: "remove",
    getGroupedRowModel: getGroupedRowModel(),
});

type MobileSalesRow =
    | { type: "date"; date: string }
    | { type: "item"; item: SalesDetail };

const flatMobileRows = computed<MobileSalesRow[]>(() => {
    const result: MobileSalesRow[] = [];
    for (const date of props.mobileSalesDateDetails) {
        result.push({ type: "date", date });
        const items = props.mobileSalesDetails[date] ?? [];
        for (const item of items) {
            result.push({ type: "item", item });
        }
    }
    return result;
});

function mobileRowKey(row: MobileSalesRow): string {
    if (row.type === "date") return `date-${row.date}`;
    return `item-${row.item.item_name}-${row.item.item_group}-${row.item.posting_date}`;
}

const mobileWrapperRef = ref<HTMLElement | null>(null);
const {
    visibleRows: mobileVisibleRows,
    topSpacerHeight: mobileTopSpacer,
    bottomSpacerHeight: mobileBottomSpacer,
} = useVisibleWindow(flatMobileRows, mobileWrapperRef, { rowHeight: 80, overscan: 8 });

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
            return formatNumber(qty, "decimal");
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
            return formatNumber(amount, "currency");
        },
        aggregationFn: "sum",
    },
];
</script>
