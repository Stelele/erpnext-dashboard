<template>
  <UModal
    v-model:open="open"
    :title="props.refund ? `Refund ${props.refund.name}` : 'Refund'"
    :ui="{ content: 'sm:max-w-2xl' }"
  >
    <template #body>
      <div class="p-4">
        <div v-if="loading" class="flex justify-center py-8">
          <UIcon name="i-lucide-loader-2" class="animate-spin text-2xl text-primary" />
        </div>

        <template v-else-if="refund">
          <div class="grid grid-cols-2 gap-x-6 gap-y-2 mb-6">
            <div class="text-sm text-muted">Customer</div>
            <div class="text-sm font-medium">{{ refund.customer_name || refund.customer }}</div>
            <div class="text-sm text-muted">Refund of</div>
            <div class="text-sm font-medium">{{ refund.return_against ?? "—" }}</div>
            <div class="text-sm text-muted">Posted</div>
            <div class="text-sm font-medium">{{ moment(`${refund.posting_date} ${refund.posting_time}`).format("DD MMM YYYY HH:mm") }}</div>
            <div class="text-sm text-muted">Total</div>
            <div class="text-sm font-medium">{{ formatNumber(refund.grand_total, "currency") }} {{ refund.currency }}</div>
            <div v-if="detail" class="text-sm text-muted">Status</div>
            <div v-if="detail">
              <UBadge class="capitalize" variant="subtle" color="neutral">
                {{ detail.status }}
              </UBadge>
            </div>
          </div>

          <UAlert
            v-if="fetchError"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="fetchError"
          />

          <template v-else-if="detail">
            <div v-if="detail.items?.length">
              <UTable
                :data="detail.items"
                :columns="itemColumns"
                :ui="{ wrapper: '' }"
              />
            </div>
            <p v-else class="text-sm text-muted text-center py-4">No items found</p>

            <div v-if="detail.payments?.length" class="mt-4">
              <p class="text-xs font-semibold text-muted uppercase tracking-wide mb-2">
                Payments
              </p>
              <div class="space-y-1">
                <div
                  v-for="payment in detail.payments"
                  :key="payment.mode_of_payment"
                  class="flex justify-between text-sm"
                >
                  <span class="text-muted">{{ payment.mode_of_payment }}</span>
                  <span class="tabular-nums">{{ formatNumber(payment.amount, "currency") }}</span>
                </div>
              </div>
            </div>
          </template>
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
import { computed, ref, watch } from "vue";
import type { TableColumn } from "@nuxt/ui";
import { ErpNextService } from "@/services/ErpNextService";
import type {
  RefundHistoryItem,
  RefundInvoiceDetail,
  RefundableItem,
} from "@/types/Refunds";
import { extractFrappeMessage } from "@/utils/FrappeError";
import { formatNumber } from "@/utils/FormatNumber";
import moment from "moment";

const props = defineProps<{
  refund: RefundHistoryItem | null;
}>();

const emit = defineEmits<{
  close: [];
}>();

const erpnext = new ErpNextService();

const loading = ref(false);
const fetchError = ref<string | null>(null);
const detail = ref<RefundInvoiceDetail | null>(null);

let requestSeq = 0;

const open = computed({
  get: () => props.refund !== null,
  set: (value: boolean) => {
    if (!value) close();
  },
});

const itemColumns: TableColumn<RefundableItem>[] = [
  {
    accessorKey: "item_name",
    header: "Item",
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
    header: "Amount",
    meta: {
      class: {
        td: "text-right font-medium tabular-nums",
      },
    },
    cell: ({ row }) => formatNumber(row.getValue("amount"), "currency"),
  },
];

watch(
  () => props.refund,
  async (refund) => {
    if (!refund) {
      detail.value = null;
      fetchError.value = null;
      return;
    }
    loading.value = true;
    fetchError.value = null;
    detail.value = null;
    const seq = ++requestSeq;
    try {
      const invoice = await erpnext.getInvoiceDetailForRefund(refund.name);
      if (seq !== requestSeq) return;
      if (!invoice) {
        fetchError.value = "Failed to load refund details.";
        return;
      }
      detail.value = invoice;
    } catch (err) {
      if (seq !== requestSeq) return;
      fetchError.value = extractFrappeMessage(err);
    } finally {
      if (seq === requestSeq) loading.value = false;
    }
  },
  { immediate: true },
);

function close() {
  emit("close");
}
</script>
