<template>
  <UModal v-model:open="open" title="Order Details">
    <template #body>
      <div class="p-4">
        <div v-if="loading" class="flex justify-center py-8">
          <UIcon name="i-lucide-loader-2" class="animate-spin text-2xl text-primary" />
        </div>

        <template v-else-if="invoice">
          <div class="grid grid-cols-2 gap-x-6 gap-y-2 mb-6">
            <div class="text-sm text-muted">Date</div>
            <div class="text-sm font-medium">{{ moment(invoice.posting_date).format("DD MMM YYYY") }}</div>
            <div class="text-sm text-muted">Supplier</div>
            <div class="text-sm font-medium">{{ invoice.supplier }}</div>
            <div class="text-sm text-muted">Invoice #</div>
            <div class="text-sm font-medium">{{ invoice.name }}</div>
            <div class="text-sm text-muted">Total</div>
            <div class="text-sm font-medium">{{ formatNumber(invoice.grand_total, "currency") }}</div>
          </div>

          <div v-if="invoice.items?.length">
            <UTable
              :data="invoice.items"
              :columns="itemColumns"
              :ui="{ wrapper: '' }"
            />
          </div>
          <p v-else class="text-sm text-muted text-center py-4">No items found</p>
        </template>

        <div class="flex justify-end pt-4">
          <UButton color="neutral" variant="outline" @click="close">
            Close
          </UButton>
        </div>
      </div>
    </template>
  </UModal>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue";
import type { TableColumn } from "@nuxt/ui";
import { ErpNextService } from "@/services/ErpNextService";
import type { PurchaseInvoiceItem, PurchaseInvoiceResponse } from "@/types/Expenses";
import { formatNumber } from "@/utils/FormatNumber";
import moment from "moment";

const toast = useToast();
const erpnext = new ErpNextService();

const props = defineProps<{
  purchaseInvoiceId: string | null;
}>();

const emit = defineEmits<{
  close: [];
}>();

const loading = ref(false);
const invoice = ref<PurchaseInvoiceResponse["data"] | null>(null);

let requestSeq = 0;

const open = computed({
  get: () => props.purchaseInvoiceId !== null,
  set: (value: boolean) => {
    if (!value) close();
  },
});

const itemColumns: TableColumn<PurchaseInvoiceItem>[] = [
  {
    accessorKey: "item_name",
    header: "Item Name",
  },
  {
    accessorKey: "qty",
    header: "Qty",
    meta: {
      class: {
        td: "text-right tabular-nums",
      },
    },
    cell: ({ row }) => formatNumber(row.getValue("qty"), "decimal"),
  },
  {
    accessorKey: "rate",
    header: "Rate",
    meta: {
      class: {
        td: "text-right tabular-nums",
      },
    },
    cell: ({ row }) => formatNumber(row.getValue("rate"), "currency"),
  },
  {
    accessorKey: "amount",
    header: "Total",
    meta: {
      class: {
        td: "text-right font-medium tabular-nums",
      },
    },
    cell: ({ row }) => formatNumber(row.getValue("amount"), "currency"),
  },
];

watch(
  () => props.purchaseInvoiceId,
  async (id) => {
    if (!id) {
      invoice.value = null;
      return;
    }
    loading.value = true;
    invoice.value = null;
    const seq = ++requestSeq;
    const resp = await erpnext.getPurchaseInvoice(id);
    if (seq !== requestSeq) return;
    loading.value = false;
    if (resp?.data) {
      invoice.value = resp.data;
    } else {
      toast.add({ title: "Error", description: "Failed to load order details", color: "error" });
      close();
    }
  },
  { immediate: true }
);

function close() {
  emit("close");
}
</script>
