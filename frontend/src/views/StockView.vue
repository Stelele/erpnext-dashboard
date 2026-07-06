<template>
    <DashboardLayout>
        <div class="col-span-6 flex flex-row-reverse gap-2">
            <UModal
                v-model:open="openReconciliation"
                title="Stock Reconciliation"
                :dismissible="false"
                :fullscreen="isMobile"
                :ui="{ content: 'sm:max-w-2xl' }"
            >
                <UButton
                    trailing-icon="i-lucide-scale"
                    class="hover:cursor-pointer"
                    >Reconcile Stock</UButton
                >
                <template #body>
                    <StockReconciliationModal :loading="reconciliationLoading" @on-submit="onReconcileSubmit" />
                </template>
            </UModal>
            <UModal
                v-model:open="openPurchase"
                title="Quick Purchase Entry"
                :dismissible="false"
                :fullscreen="isMobile"
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
            :format="item.format"
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
            @disable-item="handleDisableItem"
        />
        <DisableItemConfirmModal
            v-if="disableTarget"
            v-model:open="openDisableConfirm"
            :item-code="disableTarget.item_code"
            :item-name="disableTarget.item_name"
            :current-qty="disableTarget.real_qty"
            @confirm="onDisableConfirm"
        />
    </DashboardLayout>
</template>

<script setup lang="ts">
import type { NumberCardProps } from "@/components/NumberCard.vue";
import DashboardLayout from "@/layouts/DashboardLayout.vue";
import PurchaseForm from "@/components/PurchaseForm.vue";
import StockReconciliationModal from "@/components/StockReconciliationModal.vue";
import DisableItemConfirmModal from "@/components/DisableItemConfirmModal.vue";
import { useDataStore } from "@/stores/DataStore";
import { useStockDataStore } from "@/stores/StockDataStore";
import { useAuthStore } from "@/stores/AuthStore";
import { ErpNextService } from "@/services/ErpNextService";
import { ref, computed, onMounted, onUnmounted } from "vue";

const dataStore = useDataStore();
const stockDataStore = useStockDataStore();
const authStore = useAuthStore();

const openPurchase = ref(false);
const isMobile = ref(false);
const openReconciliation = ref(false);
const reconciliationLoading = ref(false);
const openDisableConfirm = ref(false);
const disableTarget = ref<{ item_code: string; item_name: string; real_qty: number } | null>(null);
const selectedWarehouse = ref("Stores");

function syncMobile(mq: MediaQueryList | MediaQueryListEvent) {
  isMobile.value = mq.matches;
}

onMounted(async () => {
  const mq = window.matchMedia('(max-width: 767px)');
  syncMobile(mq);
  mq.addEventListener('change', syncMobile);
  onUnmounted(() => mq.removeEventListener('change', syncMobile));

  try {
    const warehouses = await erpnext.getWarehouses();
    if (warehouses?.length) {
      const stores = warehouses.find((w: { name: string }) =>
        w.name.toLowerCase().includes("stores"),
      );
      const first = warehouses[0];
      if (first) selectedWarehouse.value = (stores || first).name;
    }
  } catch { /* ignore */ }
});

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
        dataStore.update();
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

function handleDisableItem(row: { item_code: string; item_name: string; real_qty: number }) {
  disableTarget.value = row;
  openDisableConfirm.value = true;
}

async function onReconcileSubmit(payload: {
  items: { item_code: string; qty: number }[];
  remarks?: string;
}) {
  reconciliationLoading.value = true;
  try {
    const result = await erpnext.createStockReconciliation({
      warehouse: selectedWarehouse.value,
      items: payload.items,
      company: authStore.company || "",
      remarks: payload.remarks,
    });
    if (result) {
      openReconciliation.value = false;
      dataStore.update();
      toast.add({
        title: "Stock reconciliation submitted",
        color: "success",
      });
    } else {
      toast.add({
        title: "Failed to submit stock reconciliation",
        color: "error",
      });
    }
  } catch {
    toast.add({
      title: "Failed to submit stock reconciliation",
      color: "error",
    });
  } finally {
    reconciliationLoading.value = false;
  }
}

async function onDisableConfirm(payload: { remarks?: string }) {
  if (!disableTarget.value) return;
  openDisableConfirm.value = false;
  try {
    const result = await erpnext.disableItem(
      disableTarget.value.item_code,
      selectedWarehouse.value,
      authStore.company || "",
      payload.remarks,
    );
    if (result) {
      dataStore.update();
      toast.add({
        title: `Item "${disableTarget.value.item_name}" disabled`,
        color: "success",
      });
    } else {
      toast.add({
        title: "Failed to disable item",
        color: "error",
      });
    }
  } catch {
    toast.add({
      title: "Failed to disable item",
      color: "error",
    });
  }
}
</script>
