<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                v-model:open="openPurchase"
                title="Quick Purchase Entry"
                :dismissible="false"
                :ui="{ content: 'sm:max-w-2xl' }"
            >
                <UButton
                    trailing-icon="i-lucide-shopping-cart"
                    class="hover:cursor-pointer"
                    >Quick Purchase</UButton
                >
                <template #body>
                    <PurchaseForm :loading="purchaseLoading" @on-submit="onPurchaseSubmit" />
                </template>
            </UModal>
        </div>
        <NumberCard
            v-for="(item, idx) in items"
            :key="idx"
            :title="item.title"
            :value="item.value"
            :direction="item.direction"
            :percent-change="item.percentChange"
        />
        <CardBubbleChart
            :isLoading="dataStore.loading"
            :title="stockDataStore.salesVsStock.title"
            :labels="stockDataStore.salesVsStock.labels"
            :additionalData="stockDataStore.salesVsStock.additionalData"
            :datasets="stockDataStore.salesVsStock.datasets"
            :tooltip-labels="stockDataStore.salesVsStock.tooltipLabels"
        />
        <CardDoughnutChart
            title="Stock Value By Item Group"
            :data="stockDataStore.stockByItemGroup"
        />
        <CardLineChart
            title="Daily Stock Value"
            :data="stockDataStore.dailyStockValues"
        />
        <StockTable
            :data="stockDataStore.stockTableData"
            :loading="dataStore.loading"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import type { NumberCardProps } from "@/components/NumberCard.vue";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import PurchaseForm from "@/components/PurchaseForm.vue";
import { useDataStore } from "@/stores/DataStore";
import { useStockDataStore } from "@/stores/StockDataStore";
import { useAuthStore } from "@/stores/AuthStore";
import { ErpNextService } from "@/services/ErpNextService";
import { ref, computed } from "vue";

const dataStore = useDataStore();
const stockDataStore = useStockDataStore();
const authStore = useAuthStore();

const openPurchase = ref(false);
const purchaseLoading = ref(false);
const toast = useToast();
const erpnext = new ErpNextService();

const items = computed<NumberCardProps[]>(() => [
    {
        title: "Total Stock Value ($)",
        value: stockDataStore.totalStockValue,
    },
    {
        title: "Total Selling Value ($)",
        value: stockDataStore.totalSellingValue,
    },
    {
        title: "Avg. Markup Percentage (%)",
        value: stockDataStore.averageMarkupPercentage,
    },
]);

async function onPurchaseSubmit(payload: {
    supplier: string;
    warehouse: string;
    items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
    invoice_number: string | null;
    invoice_date: string;
}) {
    purchaseLoading.value = true;
    const result = await erpnext.createFullPurchase({
        company: authStore.company || "",
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || undefined,
        invoice_date: payload.invoice_date,
    });
    purchaseLoading.value = false;

    if (result) {
        openPurchase.value = false;
        toast.add({
            title: `Purchase submitted: PO ${result.purchase_order}, PI ${result.purchase_invoice}`,
            color: "success",
        });
    } else {
        toast.add({
            title: "Failed to submit purchase",
            color: "error",
        });
    }
}
</script>
