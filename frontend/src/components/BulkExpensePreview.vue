<template>
    <div class="space-y-4">
        <UTable
            :data="importData"
            :columns="columns"
            :sticky="true"
            class="max-h-[75vh] overflow-x-auto"
        />
        <div class="flex justify-end items-center mt-4">
            <UButton color="primary" @click="submitBulkImport">
                Submit Import
            </UButton>
        </div>
    </div>
</template>

<script setup lang="ts">
import type { UniqueExpense } from "@/components/BulkExpenseUploadButton.vue";
import { formatNumber } from "@/utils/FormatNumber";
import type { TableColumn } from "@nuxt/ui";
import moment from "moment";
import { h, ref, resolveComponent, watch } from "vue";

const UButton = resolveComponent("UButton");

const importData = ref<UniqueExpense[]>([]);

const props = defineProps<{
    data?: UniqueExpense[];
}>();
const emit = defineEmits<{
    (e: "onDataSubmit", payload: UniqueExpense[]): void;
}>();

watch(
    () => props.data,
    (newData) => {
        if (newData) importData.value = newData;
    },
    { immediate: true },
);

const columns: TableColumn<UniqueExpense>[] = [
    {
        id: "date",
        accessorKey: "date",
        header: "Date",
        cell: ({ row }) => {
            return row.original.date;
        },
    },
    {
        id: "expenseType",
        accessorKey: "expenseType",
        header: "Type",
        cell: ({ row }) => {
            return row.original.expenseType;
        },
    },
    {
        id: "description",
        accessorKey: "description",
        header: "Description",
        cell: ({ row }) => {
            return row.original.description;
        },
    },
    {
        id: "amount",
        accessorKey: "amount",
        header: "Amount",
        meta: {
            class: {
                th: "text-right",
                td: "text-right font-medium",
            },
        },
        cell: ({ row }) => {
            return formatNumber(row.original.amount, "currency");
        },
    },
    {
        id: "actions",
        cell: ({ row }) =>
            h(
                UButton,
                {
                    color: "error",
                    variant: "subtle",
                    icon: "i-lucide-trash",
                    onClick: () => removeRow(row.original.id),
                },
                "Delete",
            ),
    },
];

function removeRow(idToRemove: string) {
    importData.value = importData.value.filter((row) => row.id !== idToRemove);
}

function addRow() {
    importData.value.push({
        id: Date.now().toString(),
        date: moment().format("YYYY-MM-DD"),
        expenseType: "Sekuru",
        amount: 0,
        description: "",
    });
}

function submitBulkImport() {
    emit("onDataSubmit", importData.value);
}
</script>
