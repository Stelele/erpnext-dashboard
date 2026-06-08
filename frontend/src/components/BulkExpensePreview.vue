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
import type { CompanyExpenseMapping } from "@/types/Expenses";
import { formatNumber } from "@/utils/FormatNumber";
import type { TableColumn } from "@nuxt/ui";
import { h, ref, resolveComponent, watch, computed } from "vue";

const UButton = resolveComponent("UButton");

const importData = ref<UniqueExpense[]>([]);

const props = defineProps<{
    data?: UniqueExpense[];
    mappings?: CompanyExpenseMapping[];
}>();
const emit = defineEmits<{
    (e: "onDataSubmit", payload: UniqueExpense[]): void;
}>();

const mappingLookup = computed(() => {
    const lookup: Record<string, string> = {};
    props.mappings?.forEach((m) => {
        lookup[m.expenseTypeId] = m.expenseTypeName;
    });
    return lookup;
});

const nameToIdLookup = computed(() => {
    const lookup: Record<string, string> = {};
    props.mappings?.forEach((m) => {
        lookup[m.expenseTypeName.toLowerCase()] = m.expenseTypeId;
    });
    return lookup;
});

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
        accessorKey: "expenseTypeId",
        header: "Type",
        cell: ({ row }) => {
            return mappingLookup.value[row.original.expenseTypeId] ?? row.original.expenseTypeId;
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

function resolveExpenseTypeId(expense: UniqueExpense): string {
    const direct = expense.expenseTypeId;
    if (direct && direct.includes("-")) {
        return direct;
    }
    const resolved = nameToIdLookup.value[direct.toLowerCase()];
    return resolved ?? direct;
}

function submitBulkImport() {
    const resolved = importData.value.map((expense) => ({
        ...expense,
        expenseTypeId: resolveExpenseTypeId(expense),
    }));
    emit("onDataSubmit", resolved);
}
</script>
