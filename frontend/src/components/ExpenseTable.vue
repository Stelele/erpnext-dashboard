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
                :ui="{ tr: 'data-[expanded=true]:bg-elevated/50' }"
                class="flex-1 h-full"
                loadingColor="primary"
            >
                <template #expanded="{ row }">
                    <div class="grid grid-cols-2 w-full md:w-1/2 px-1 md:px-4">
                        <div>Date</div>
                        <div>
                            {{
                                moment(row.original.date).format("DD MMM YYYY")
                            }}
                        </div>
                        <div>#</div>
                        <div>{{ row.original.id }}</div>
                        <div>Status</div>
                        <div>
                            <UBadge
                                class="capitalize"
                                variant="subtle"
                                :color="getStatusColor(row.original.status)"
                                >{{ row.original.status }}</UBadge
                            >
                        </div>
                        <div>Type</div>
                        <div>{{ row.original.type }}</div>
                        <div>Desciption</div>
                        <div class="text-wrap">
                            {{ row.original.description }}
                        </div>
                        <div>Amount</div>
                        <div>
                            {{ formatNumber(row.original.amount, "currency") }}
                        </div>
                    </div>
                </template>
            </UTable>
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { h, ref, resolveComponent } from "vue";
import type { TableColumn } from "@nuxt/ui";
import type { Payment } from "@/types/Expenses";
import moment from "moment";
import { formatNumber } from "@/utils/FormatNumber";

const UBadge = resolveComponent("UBadge");
const UButton = resolveComponent("UButton");

const props = defineProps<{
    data: Payment[];
    loading: boolean;
}>();

const expanded = ref({});

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
    }[status];
}
</script>
