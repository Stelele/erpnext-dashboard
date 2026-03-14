<template>
    <div class="w-full flex justify-center items-center">
        <UFileUpload
            v-model="fileUpload"
            @update:modelValue="handleFileUpload"
            accept=".json, .xlsx, .xls, .sqlite, .db"
            class="w-96 min-h-48"
        />
    </div>
</template>

<script setup lang="ts">
import * as XLSX from "xlsx";
import initSqlJs from "sql.js";
import type { SqlJsStatic, QueryExecResult } from "sql.js";
import type { Expense, ExpenseType } from "@/types/Expenses";
import moment from "moment";
import { ref } from "vue";

export type UniqueExpense = Expense & { id: string };

const fileUpload = ref<File | null>(null);
const emit = defineEmits<{
    (e: "onDataExtracted", payload: UniqueExpense[]): void;
    (e: "error", message: string): void;
}>();

const SQLITE_QUERY = `
  SELECT
    e.id,
    e.bookname,
    e.enteramount,
    e.partyname,
    e.date,
    e.time,
    c.categoryName
  FROM entry e
  LEFT JOIN CashOutCategory c
    ON e.categoryId = c.id
  WHERE
    e.bookname = ?
    AND e.plusminus = 'false'
    AND c.categoryName != 'Orders'
    AND e.id > ?`;
const validExpenseTypes: ExpenseType[] = [
    "Canteen",
    "Consumables",
    "Other",
    "Sekuru",
    "Spoiled Meat",
    "Staff",
    "Utilities",
];

const handleFileUpload = async (file: File | null | undefined) => {
    if (!file) return;

    const extension = file.name.split(".").pop()?.toLowerCase();
    let extractedData: UniqueExpense[] = [];

    try {
        if (extension === "json") {
            extractedData = await parseJSON(file);
        } else if (extension === "xlsx" || extension === "xls") {
            extractedData = await parseExcel(file);
        } else {
            extractedData = await parseSQLite(file, SQLITE_QUERY);
        }

        const filteredData = extractedData
            .filter((e) => validExpenseTypes.includes(e.expenseType))
            .sort((a, b) => {
                const dateA = moment(a.date, "YYYY-MM-DD");
                const dateB = moment(b.date, "YYYY-MM-DD");
                return dateA.diff(dateB, "days");
            });
        emit("onDataExtracted", filteredData);
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

                resolve(arrayData);
            } catch (err) {
                reject(new Error("Failed to parse JSON content"));
            }
        };
        reader.onerror = () => reject(new Error("Failed to read JSON file"));
        reader.readAsText(file);
    });
};

const parseExcel = (file: File): Promise<UniqueExpense[]> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => {
            try {
                const data = new Uint8Array(e.target?.result as ArrayBuffer);
                const workbook = XLSX.read(data, { type: "array" });

                const firstSheetName = workbook.SheetNames[0] ?? "";
                const worksheet = workbook.Sheets[firstSheetName];

                if (!worksheet)
                    throw new Error("No sheets found in the workbook");

                const rows = XLSX.utils.sheet_to_json<UniqueExpense>(worksheet);

                resolve(rows);
            } catch (err) {
                reject(new Error("Failed to parse Excel file"));
            }
        };
        reader.onerror = () => reject(new Error("Failed to read Excel file"));

        reader.readAsArrayBuffer(file);
    });
};

const parseSQLite = async (
    file: File,
    query: string,
): Promise<UniqueExpense[]> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = async (e) => {
            try {
                const SQL: SqlJsStatic = await initSqlJs({
                    locateFile: () => `/sql-wasm.wasm`,
                });

                const Uints = new Uint8Array(e.target?.result as ArrayBuffer);
                const db = new SQL.Database(Uints);

                const res: QueryExecResult[] = db.exec(query, ["Ecocash", 0]);

                if (res.length > 0) {
                    const columns = res[0]?.columns;
                    const formattedData: Record<string, any>[] =
                        res[0]?.values.map((row) => {
                            const obj = {} as Record<string, any>;
                            columns?.forEach((col, index) => {
                                obj[col] = row[index];
                            });
                            return obj;
                        }) ?? [];

                    const parsedData = formattedData.map((e) => ({
                        id: e.id,
                        date: moment(e.date, "DD MMM YYYY").format(
                            "YYYY-MM-DD",
                        ),
                        expenseType: e.categoryName,
                        amount: parseFloat(e.enteramount),
                        description: e.partyname,
                    }));

                    resolve(parsedData);
                } else {
                    resolve([]);
                }
            } catch (err) {
                reject(
                    new Error("Failed to parse SQLite file or execute query"),
                );
            }
        };
        reader.onerror = () => reject(new Error("Failed to read SQLite file"));
        reader.readAsArrayBuffer(file);
    });
};
</script>
