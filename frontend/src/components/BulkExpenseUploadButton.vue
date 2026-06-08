<template>
    <div class="w-full flex justify-center items-center">
        <UFileUpload
            v-model="fileUpload"
            @update:modelValue="handleFileUpload"
            accept=".json"
            class="w-96 min-h-48"
        />
    </div>
</template>

<script setup lang="ts">
import type { Expense } from "@/types/Expenses";
import { ref } from "vue";

export type UniqueExpense = Expense & { id: string };

const fileUpload = ref<File | null>(null);
const emit = defineEmits<{
    (e: "onDataExtracted", payload: UniqueExpense[]): void;
    (e: "error", message: string): void;
}>();

const handleFileUpload = async (file: File | null | undefined) => {
    if (!file) return;

    const extension = file.name.split(".").pop()?.toLowerCase();
    let extractedData: UniqueExpense[] = [];

    try {
        if (extension === "json") {
            extractedData = await parseJSON(file);
        } else {
            emit("error", "Unsupported file format. Please upload a JSON file.");
            return;
        }

        extractedData.sort((a, b) => {
            const dateA = new Date(a.date);
            const dateB = new Date(b.date);
            return dateA.getTime() - dateB.getTime();
        });
        emit("onDataExtracted", extractedData);
    } catch (error: any) {
        console.error("Extraction failed:", error);
        emit("error", error.message || "An unknown error occurred");
    } finally {
        fileUpload.value = null;
    }
};

const parseJSON = (file: File): Promise<UniqueExpense[]> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const result = e.target?.result as string;
                const parsedData = JSON.parse(result);
                const arrayData = Array.isArray(parsedData)
                    ? parsedData
                    : [parsedData];

                const expenses: UniqueExpense[] = arrayData.map((item: any, index: number) => ({
                    id: item.id ?? `import-${index}`,
                    date: item.date,
                    expenseTypeId: item.expenseTypeId ?? item.expenseType ?? "",
                    amount: Number(item.amount),
                    description: item.description ?? "",
                }));

                resolve(expenses);
            } catch (err) {
                reject(new Error("Failed to parse JSON content"));
            }
        };
        reader.onerror = () => reject(new Error("Failed to read JSON file"));
        reader.readAsText(file);
    });
};
</script>
