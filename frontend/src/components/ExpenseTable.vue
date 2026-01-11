<template>
    <UPageCard
        class="min-h-96 col-span-6"
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
                :data="props.data"
                :columns="columns"
                :loading="props.loading"
                loadingColor="primary"
            />
        </div>
    </UPageCard>
</template>

<script setup lang="ts">
import { h, resolveComponent } from "vue";
import type { TableColumn } from "@nuxt/ui";
import type { Payment } from "@/types/Expenses";
import moment from "moment";

const UBadge = resolveComponent("UBadge");

const props = defineProps<{
    data: Payment[];
    loading: boolean;
}>();

const columns: TableColumn<Payment>[] = [
    {
        accessorKey: "id",
        header: "#",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "hidden md:table-cell",
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
        cell: ({ row }) => {
            const color = {
                Submitted: "success" as const,
                Cancelled: "error" as const,
                Draft: "neutral" as const,
            }[row.getValue("status") as string];

            return h(
                UBadge,
                { class: "capitalize", variant: "subtle", color },
                () => row.getValue("status"),
            );
        },
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
                td: "max-w-[100px] md:max-w-[200px] lg:max-w-[300px] overflow-hidden text-ellipsis whitespace-nowrap",
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
            return new Intl.NumberFormat("en-US", {
                style: "currency",
                currency: "USD",
            }).format(amount);
        },
    },
];
</script>
