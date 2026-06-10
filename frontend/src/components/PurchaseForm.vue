<template>
  <div class="p-4">
    <div v-if="!showConfirm">
    <UForm :schema="schema" :state="state" class="space-y-4" @submit="onSubmit">
      <div class="grid grid-cols-2 gap-4">
        <UFormField label="Supplier" name="supplier" required>
          <UInputMenu
            v-model="selectedSupplier"
            :items="supplierItems"
            value-key="name"
            label-key="supplier_name"
            placeholder="Search supplier..."
            class="w-full"
            :disabled="submitting"
          />
        </UFormField>
        <UFormField label="Warehouse" name="warehouse" required>
          <UInputMenu
            v-model="selectedWarehouse"
            :items="warehouseOpts"
            value-key="name"
            label-key="name"
            class="w-full"
            :disabled="submitting"
          />
        </UFormField>
        <UFormField label="Invoice No.">
          <UInput v-model="state.invoiceNumber" placeholder="Optional" class="w-full" :disabled="submitting" />
        </UFormField>
        <UFormField label="Invoice Date">
          <UPopover>
            <UButton color="neutral" variant="subtle" icon="i-lucide-calendar" class="w-full" :disabled="submitting">
              {{ displayDate }}
            </UButton>
            <template #content>
              <UCalendar v-model="state.invoiceDate" class="p-2" />
            </template>
          </UPopover>
        </UFormField>
      </div>

      <div>
        <label class="text-sm font-medium mb-2 block">Items</label>
        <div class="grid grid-cols-[2fr_1fr_1fr_1fr_100px_auto] gap-2 mb-2 text-xs font-medium text-[var(--ui-text-dimmed)] px-1">
          <span>Product</span>
          <span>Qty</span>
          <span>Rate</span>
          <span>Sell</span>
          <span>Total</span>
          <span></span>
        </div>
        <div v-for="(_, idx) in state.items" :key="idx" class="grid grid-cols-[2fr_1fr_1fr_1fr_100px_auto] gap-2 mb-2">
          <UInputMenu
            v-model="itemSelections[idx]"
            v-model:search-term="itemSearchTerms[idx]"
            :items="itemOpts[idx]"
            value-key="item_code"
            label-key="item_name"
            description-key="description"
            :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
            class="w-full"
            :disabled="submitting"
            @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
            @update:model-value="() => onItemPicked(idx)"
          />
          <UInput v-model="state.items[idx].qty" type="number" :min="1" :step="1" class="w-full" :disabled="submitting" />
          <UInput v-model="state.items[idx].rate" type="number" :min="0" :step="0.01" class="w-full" :disabled="submitting" />
          <UInput v-model="state.items[idx].sell_rate" type="number" :min="0" :step="0.01" class="w-full" :disabled="submitting" />
          <div class="flex items-center justify-end text-sm font-medium">
            {{ ((state.items[idx].qty || 0) * (state.items[idx].rate || 0)).toFixed(2) }}
          </div>
          <UButton color="error" variant="ghost" icon="i-lucide-x" size="sm" :disabled="submitting" @click="removeItem(idx)" />
        </div>
        <UButton variant="outline" icon="i-lucide-plus" size="sm" class="w-full" :disabled="submitting" @click="addItem">
          Add Item
        </UButton>
      </div>

      <div class="flex justify-between items-center pt-4 border-t border-[var(--ui-border)]">
        <span class="text-lg font-bold">Total: {{ grandTotal.toFixed(2) }}</span>
        <UButton type="submit" color="primary" :loading="submitting" :disabled="submitting">
          Submit Purchase
        </UButton>
      </div>
    </UForm>
    </div>
    <div v-else class="space-y-4">
      <div class="text-sm text-[var(--ui-text)]">
        <p class="font-medium text-base mb-2">Confirm Purchase</p>
        <p>This will create 4 documents: Purchase Order, Purchase Receipt, Purchase Invoice, and a Cash Payment Entry. <strong>Stock levels and accounting ledgers will be updated immediately.</strong></p>
      </div>
      <div class="bg-[var(--ui-bg-elevated)] rounded-lg p-5 space-y-4 text-sm">
        <div>
          <p class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3">Order Details</p>
          <div class="space-y-2.5">
            <div v-if="supplierLabel" class="flex justify-between">
              <span class="text-[var(--ui-text-muted)]">Supplier</span>
              <span class="font-medium">{{ supplierLabel }}</span>
            </div>
            <div v-if="warehouseLabel" class="flex justify-between">
              <span class="text-[var(--ui-text-muted)]">Warehouse</span>
              <span class="font-medium">{{ warehouseLabel }}</span>
            </div>
            <div v-if="state.invoiceNumber" class="flex justify-between">
              <span class="text-[var(--ui-text-muted)]">Invoice No.</span>
              <span class="font-medium">{{ state.invoiceNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-[var(--ui-text-muted)]">Invoice Date</span>
              <span class="font-medium">{{ displayDate }}</span>
            </div>
          </div>
        </div>

        <USeparator />

        <div>
          <p class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3">Items</p>
          <div class="space-y-1.5">
            <div v-for="(item, idx) in validItems" :key="idx" class="flex justify-between">
              <span class="font-medium">{{ item.item_name }}</span>
              <span class="text-[var(--ui-text-muted)]">{{ item.qty }} × {{ item.rate.toFixed(2) }} = {{ (item.qty * item.rate).toFixed(2) }}</span>
            </div>
          </div>
        </div>

        <USeparator />

        <div class="flex justify-between items-center">
          <span class="font-semibold">Total</span>
          <span class="text-base font-bold">{{ grandTotal.toFixed(2) }}</span>
        </div>
      </div>
      <div class="flex justify-end gap-2">
        <UButton color="neutral" variant="outline" @click="showConfirm = false">
          Back
        </UButton>
        <UButton color="primary" :loading="submitting" @click="confirmSubmit">
          Confirm & Submit
        </UButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { CalendarDate, getLocalTimeZone } from "@internationalized/date";
import * as z from "zod";
import moment from "moment";
import { computed, reactive, ref, shallowRef, watch, onMounted } from "vue";
import { ErpNextService, type ItemOption, type SupplierOption, type WarehouseOption } from "@/services/ErpNextService";

const toast = useToast();

const showConfirm = ref(false);
const supplierLabel = computed(() => {
  const s = selectedSupplier.value;
  return typeof s === 'string' ? s : s?.supplier_name || "";
});
const warehouseLabel = computed(() => {
  const w = selectedWarehouse.value;
  return typeof w === 'string' ? w : w?.name || "";
});
const validItems = computed(() => state.items.filter((i) => i.item_code));

const emit = defineEmits<{
  onSubmit: [
    payload: {
      supplier: string;
      warehouse: string;
      items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
      invoice_number: string | null;
      invoice_date: string;
    },
  ];
}>();

const props = defineProps<{
  loading?: boolean;
}>();

const submitting = computed(() => props.loading ?? false);
const erpnext = new ErpNextService();

// --- Supplier ---
const selectedSupplier = ref<SupplierOption | null>(null);
const supplierItems = ref<SupplierOption[]>([]);

// --- Warehouse ---
const warehouseItems = ref<WarehouseOption[]>([]);
const warehouseOpts = computed(() => warehouseItems.value.map((w) => ({ name: w.name })));
const selectedWarehouse = ref<{ name: string } | undefined>();

// --- Items ---
interface PurchaseItem {
  item_code: string;
  item_name: string;
  qty: number;
  rate: number;
  sell_rate: number;
}

const itemSelections = ref<(ItemOption | null)[]>([null]);
const itemOpts = ref<ItemOption[][]>([[]]);
const itemSearchTerms = ref<string[]>([""]);
const itemTimers: Record<number, ReturnType<typeof setTimeout>> = {};

const state = reactive({
  invoiceNumber: "",
  invoiceDate: shallowRef(new CalendarDate(moment().year(), moment().month() + 1, moment().date())),
  items: [{ item_code: "", item_name: "", qty: 1, rate: 0, sell_rate: 0 }] as PurchaseItem[],
});

const schema = z.object({
  invoiceNumber: z.string().optional(),
  invoiceDate: z.object({ year: z.number(), month: z.number(), day: z.number() }).transform(
    ({ year, month, day }) => shallowRef(new CalendarDate(year, month, day)),
  ),
  items: z.array(z.object({
    item_code: z.string().optional(),
    item_name: z.string().optional(),
    qty: z.number().gt(0, "Qty must be > 0"),
    rate: z.number().gte(0, "Rate must be >= 0"),
    sell_rate: z.number().gte(0, "Sell rate must be >= 0"),
  })).min(1, "At least one item required"),
});

const displayDate = computed(() =>
  moment(state.invoiceDate.toDate(getLocalTimeZone())).format("DD MMM YYYY"),
);

const grandTotal = computed(() =>
  state.items.reduce((sum, item) => sum + (item.qty || 0) * (item.rate || 0), 0),
);

onMounted(async () => {
  try {
    const warehouses = await erpnext.getWarehouses();
    if (warehouses?.length) {
      warehouseItems.value = warehouses;
      const stores = warehouses.find((w) => w.name.toLowerCase().includes("stores"));
      selectedWarehouse.value = { name: (stores || warehouses[0]).name };
    }
  } catch { /* ignore */ }

  try {
    const suppliers = await erpnext.searchSuppliers("");
    if (suppliers) supplierItems.value = suppliers;
  } catch { /* ignore */ }

  // Preload items for first row
  try {
    const items = await erpnext.searchItems("");
    if (items) itemOpts.value[0] = items;
  } catch { /* ignore */ }
});

function watchRow(idx: number) {
  watch(() => itemSearchTerms.value[idx], (term) => {
    if (itemTimers[idx]) clearTimeout(itemTimers[idx]);
    itemTimers[idx] = setTimeout(async () => {
      try {
        const results = await erpnext.searchItems(term);
        if (results) itemOpts.value[idx] = results;
      } catch { /* ignore */ }
    }, 300);
  });
}

watchRow(0);

watch(submitting, (v) => {
  if (v) showConfirm.value = false;
});

async function onItemOpen(idx: number) {
  if (!itemOpts.value[idx]?.length) {
    try {
      const results = await erpnext.searchItems("");
      if (results) itemOpts.value[idx] = results;
    } catch { /* ignore */ }
  }
}

function onItemPicked(idx: number) {
  const itemCode = itemSelections.value[idx] as unknown as string;
  if (!itemCode) return;
  const sel = itemOpts.value[idx]?.find((i) => i.item_code === itemCode);
  if (sel) {
    state.items[idx].item_code = sel.item_code;
    state.items[idx].item_name = sel.item_name;
    if (sel.last_purchase_rate) state.items[idx].rate = sel.last_purchase_rate;
    if (sel.last_selling_rate) state.items[idx].sell_rate = sel.last_selling_rate;
  }
}

function addItem() {
  state.items.push({ item_code: "", item_name: "", qty: 1, rate: 0, sell_rate: 0 });
  itemSelections.value.push(null);
  itemOpts.value.push([]);
  itemSearchTerms.value.push("");
  const newIdx = state.items.length - 1;
  watchRow(newIdx);
  onItemOpen(newIdx);
}

function removeItem(idx: number) {
  if (state.items.length > 1) {
    state.items.splice(idx, 1);
    itemSelections.value.splice(idx, 1);
    itemOpts.value.splice(idx, 1);
    itemSearchTerms.value.splice(idx, 1);
    if (itemTimers[idx]) {
      clearTimeout(itemTimers[idx]);
      delete itemTimers[idx];
    }
  }
}

async function onSubmit() {
  if (!selectedSupplier.value) {
    toast.add({ title: "Please select a supplier", color: "error" });
    return;
  }

  if (validItems.value.length === 0) {
    toast.add({ title: "Please add at least one product", color: "error" });
    return;
  }

  showConfirm.value = true;
}

function confirmSubmit() {
  emit("onSubmit", {
    supplier: typeof selectedSupplier.value === 'string' ? selectedSupplier.value : selectedSupplier.value?.name || "",
    warehouse: typeof selectedWarehouse.value === 'string' ? selectedWarehouse.value : selectedWarehouse.value?.name || "",
    items: validItems.value.map((i) => ({ item_code: i.item_code, qty: i.qty, rate: i.rate, sell_rate: i.sell_rate })),
    invoice_number: state.invoiceNumber || null,
    invoice_date: moment(state.invoiceDate.toDate(getLocalTimeZone())).format("YYYY-MM-DD"),
  });
}
</script>
