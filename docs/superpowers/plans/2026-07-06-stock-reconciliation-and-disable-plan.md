# Stock Reconciliation & Item Disable — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add stock reconciliation modal and item disable button to the Stock page, both creating real ERPNext entries via Frappe REST API.

**Architecture:** Two new modals hosted in StockView (following the Purchase modal pattern), two new methods in ErpNextService for API calls, and an actions column + expanded-row button in StockTable. All following existing Nuxt UI patterns (UModal, UForm, Zod, 2-step confirmation).

**Tech Stack:** Vue 3 + Nuxt UI + Zod + Axios (to Frappe REST API)

---

### Task 1: Add stock reconciliation and disable item methods to ErpNextService

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts` (append before closing brace of the class)

- [ ] **Step 1: Add `createStockReconciliation` method**

Insert after line 466 (after `cancelExpenseJournalEntry`), before `getJournalEntry`:

```ts
  public async createStockReconciliation(payload: {
    warehouse: string;
    items: { item_code: string; qty: number }[];
    company: string;
    remarks?: string;
  }): Promise<boolean> {
    try {
      const now = moment();
      const body: Record<string, unknown> = {
        company: payload.company,
        set_posting_time: 1,
        posting_date: now.format("YYYY-MM-DD"),
        posting_time: now.format("HH:mm:ss"),
        items: payload.items.map((i) => ({
          item_code: i.item_code,
          warehouse: payload.warehouse,
          qty: i.qty,
        })),
      };
      if (payload.remarks) {
        body.remarks = payload.remarks;
      }
      const createResp = await this.instance.post(
        "/api/resource/Stock Reconciliation",
        body,
      );
      if (createResp.status !== 200) return false;
      const name = createResp.data.data.name;
      await this.instance.put(
        `/api/resource/Stock Reconciliation/${name}`,
        { docstatus: 1 },
      );
      return true;
    } catch {
      return false;
    }
  }
```

- [ ] **Step 2: Add `disableItem` method**

Insert after the `createStockReconciliation` method:

```ts
  public async disableItem(
    itemCode: string,
    warehouse: string,
    company: string,
    remarks?: string,
  ): Promise<boolean> {
    try {
      const reconResult = await this.createStockReconciliation({
        warehouse,
        company,
        items: [{ item_code: itemCode, qty: 0 }],
        remarks: remarks || `Disabling item: ${itemCode}`,
      });
      if (!reconResult) return false;
      await this.instance.put(`/api/resource/Item/${encodeURIComponent(itemCode)}`, {
        disabled: 1,
      });
      return true;
    } catch {
      return false;
    }
  }
```

- [ ] **Step 3: Verify build**

Run: `npm run build` from `frontend/`
Expected: No TypeScript or build errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add createStockReconciliation and disableItem to ErpNextService"
```

---

### Task 2: Add actions column and disable emit to StockTable

**Files:**
- Modify: `frontend/src/components/StockTable.vue`

- [ ] **Step 1: Add `defineEmits`**

After the existing `defineProps<Props>()` line (line 102), add:

```ts
const emit = defineEmits<{
  disableItem: [row: StockRow];
}>();
```

- [ ] **Step 2: Add the actions column**

Insert after the `total_gross_profit` column (after line 225, before `];` at line 226):

```ts
    {
        id: "actions",
        meta: {
            class: {
                th: "hidden md:table-cell",
                td: "hidden md:table-cell",
            },
        },
        cell: ({ row }) =>
            h(UButton, {
                color: "error",
                variant: "ghost",
                icon: "i-lucide-ban",
                square: true,
                onClick: () => emit("disableItem", row.original),
            }),
    },
```

- [ ] **Step 3: Add disable button to expanded row**

In the template, after the closing `</div>` of the expanded row detail grid (after line 78, before the closing `</template>` at line 80), add:

```html
                    <div class="col-span-2 pt-3 mt-3 border-t border-[var(--ui-border)]">
                        <UButton
                            color="error"
                            variant="outline"
                            icon="i-lucide-ban"
                            size="sm"
                            @click="emit('disableItem', row.original)"
                        >
                            Disable Item
                        </UButton>
                    </div>
```

- [ ] **Step 4: Verify build**

Run: `npm run build` from `frontend/`
Expected: No TypeScript or build errors.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/StockTable.vue
git commit -m "feat: add actions column and disable-item emit to StockTable"
```

---

### Task 3: Create DisableItemConfirmModal component

**Files:**
- Create: `frontend/src/components/DisableItemConfirmModal.vue`

- [ ] **Step 1: Create the component**

```vue
<template>
    <UModal
        v-model:open="open"
        title="Disable Item"
        :dismissible="false"
    >
        <template #body>
            <div class="p-4 space-y-4">
                <div class="bg-[var(--ui-bg-elevated)] rounded-lg p-4 space-y-2 text-sm">
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Item</span>
                        <span class="font-medium">{{ props.itemName }}</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Code</span>
                        <span class="font-medium">{{ props.itemCode }}</span>
                    </div>
                    <div class="flex justify-between">
                        <span class="text-[var(--ui-text-muted)]">Current Quantity</span>
                        <span class="font-medium">{{ props.currentQty }}</span>
                    </div>
                </div>

                <div
                    class="text-sm text-[var(--ui-text-dimmed)] bg-[var(--ui-bg-elevated)] rounded-lg p-4"
                >
                    <p>This will zero out stock via a reconciliation and disable the item in ERPNext. <strong>This cannot be undone.</strong></p>
                </div>

                <UFormField label="Remarks (optional)">
                    <UTextarea
                        v-model="remarks"
                        placeholder="Reason for disabling..."
                        :rows="3"
                    />
                </UFormField>

                <div class="flex justify-end gap-2 pt-2">
                    <UButton
                        color="neutral"
                        variant="outline"
                        @click="open = false"
                    >
                        Cancel
                    </UButton>
                    <UButton
                        color="error"
                        @click="onConfirm"
                    >
                        Disable Item
                    </UButton>
                </div>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";

const props = defineProps<{
    itemCode: string;
    itemName: string;
    currentQty: number;
}>();

const emit = defineEmits<{
    confirm: [{ remarks?: string }];
}>();

const open = defineModel<boolean>("open", { default: false });
const remarks = ref("");

watch(open, (isOpen) => {
    if (isOpen) {
        remarks.value = "";
    }
});

function onConfirm() {
    emit("confirm", { remarks: remarks.value || undefined });
}
</script>
```

- [ ] **Step 2: Verify build**

Run: `npm run build` from `frontend/`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/DisableItemConfirmModal.vue
git commit -m "feat: add DisableItemConfirmModal component"
```

---

### Task 4: Create StockReconciliationModal component

**Files:**
- Create: `frontend/src/components/StockReconciliationModal.vue`

- [ ] **Step 1: Create the component**

```vue
<template>
    <div class="p-4">
        <div v-if="!showConfirm">
            <UForm
                :schema="schema"
                :state="state"
                class="space-y-4"
                @submit="onFormSubmit"
            >
                <UFormField label="Remarks (optional)">
                    <UTextarea
                        v-model="state.remarks"
                        placeholder="Reason for reconciliation..."
                        :rows="2"
                        :disabled="submitting"
                    />
                </UFormField>

                <!-- Desktop -->
                <div class="hidden md:block">
                    <div class="mt-4">
                        <div
                            class="grid grid-cols-[2fr_1fr_1fr_100px_auto] gap-2 mb-2 text-xs font-medium text-[var(--ui-text-dimmed)] px-1"
                        >
                            <span>Item</span>
                            <span>Current Qty</span>
                            <span>Corrected Qty</span>
                            <span>Difference</span>
                            <span></span>
                        </div>
                        <div
                            v-for="(item, idx) in state.items"
                            :key="idx"
                            class="grid grid-cols-[2fr_1fr_1fr_100px_auto] gap-2 mb-2"
                        >
                            <UInputMenu
                                v-model="itemSelections[idx] as any"
                                v-model:search-term="itemSearchTerms[idx]"
                                :items="itemOptions[idx]"
                                value-key="item_code"
                                label-key="item_name"
                                :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
                                class="w-full"
                                :ignore-filter="true"
                                :disabled="submitting"
                                @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                                @update:model-value="() => onItemPicked(idx)"
                            />
                            <div class="flex items-center text-sm">
                                {{ formatNumber(item.current_qty, "decimal") }}
                            </div>
                            <UInput
                                v-model="item.corrected_qty"
                                type="number"
                                :min="0"
                                :step="0.01"
                                class="w-full"
                                :disabled="submitting"
                            />
                            <div
                                class="flex items-center justify-end text-sm font-medium"
                                :class="diffClass(idx)"
                            >
                                {{ formatDiff(idx) }}
                            </div>
                            <UButton
                                color="error"
                                variant="ghost"
                                icon="i-lucide-x"
                                size="sm"
                                :disabled="submitting || state.items.length <= 1"
                                @click="removeItem(idx)"
                            />
                        </div>
                        <UButton
                            variant="outline"
                            icon="i-lucide-plus"
                            size="sm"
                            class="w-full"
                            :disabled="submitting"
                            @click="addItem"
                        >
                            Add Item
                        </UButton>
                    </div>
                </div>

                <!-- Mobile -->
                <div class="md:hidden space-y-4">
                    <div>
                        <div class="flex items-center justify-between mb-3">
                            <span class="text-sm font-medium">Items</span>
                            <UButton
                                variant="outline"
                                icon="i-lucide-plus"
                                size="xs"
                                :disabled="submitting"
                                @click="addItem"
                            >
                                Add
                            </UButton>
                        </div>
                        <div class="space-y-3">
                            <UCard
                                v-for="(item, idx) in state.items"
                                :key="idx"
                            >
                                <template #header>
                                    <div class="flex justify-between items-center gap-2">
                                        <USelectMenu
                                            v-model="itemSelections[idx] as any"
                                            v-model:search-term="itemSearchTerms[idx]"
                                            :items="itemOptions[idx]"
                                            value-key="item_code"
                                            label-key="item_name"
                                            :placeholder="itemSelections[idx]?.item_name || 'Search item...'"
                                            :ignore-filter="true"
                                            :search-input="{ placeholder: 'Search items...' }"
                                            class="flex-1"
                                            :disabled="submitting"
                                            @update:open="(open: boolean) => { if (open) onItemOpen(idx) }"
                                            @update:model-value="() => onItemPicked(idx)"
                                        />
                                        <UButton
                                            color="error"
                                            variant="ghost"
                                            icon="i-lucide-x"
                                            size="sm"
                                            :disabled="submitting || state.items.length <= 1"
                                            @click="removeItem(idx)"
                                        />
                                    </div>
                                </template>
                                <div class="grid grid-cols-3 gap-2">
                                    <UFormField label="Current Qty" size="xs">
                                        <div class="h-9 flex items-center text-sm">
                                            {{ formatNumber(item.current_qty, "decimal") }}
                                        </div>
                                    </UFormField>
                                    <UFormField label="Corrected Qty" size="xs">
                                        <UInput
                                            v-model="item.corrected_qty"
                                            type="number"
                                            :min="0"
                                            :step="0.01"
                                            :disabled="submitting"
                                        />
                                    </UFormField>
                                    <UFormField label="Diff" size="xs">
                                        <div
                                            class="h-9 flex items-center text-sm font-medium"
                                            :class="diffClass(idx)"
                                        >
                                            {{ formatDiff(idx) }}
                                        </div>
                                    </UFormField>
                                </div>
                            </UCard>
                        </div>
                    </div>
                </div>

                <div
                    class="flex justify-end gap-2 pt-4 border-t border-[var(--ui-border)]"
                >
                    <UButton
                        type="submit"
                        color="primary"
                        :loading="submitting"
                        :disabled="submitting"
                    >
                        Review & Submit
                    </UButton>
                </div>
            </UForm>
        </div>

        <div v-else class="space-y-4">
            <div class="text-sm text-[var(--ui-text)]">
                <p class="font-medium text-base mb-2">
                    Confirm Stock Reconciliation
                </p>
                <p>
                    This will create a Stock Reconciliation entry in ERPNext.
                    <strong>Stock levels will be updated immediately.</strong>
                </p>
            </div>

            <div
                class="bg-[var(--ui-bg-elevated)] rounded-lg p-5 space-y-4 text-sm"
            >
                <div v-if="validItems.length > 0">
                    <p
                        class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3"
                    >
                        Items
                    </p>
                    <div class="space-y-1.5">
                        <div
                            v-for="(item, idx) in validItems"
                            :key="idx"
                            class="flex justify-between"
                        >
                            <span class="font-medium">{{ item.item_name }}</span>
                            <span class="text-[var(--ui-text-muted)]">
                                {{ formatNumber(item.current_qty, "decimal") }} &rarr;
                                {{ formatNumber(item.corrected_qty, "decimal") }}
                                ({{ formatDiff(state.items.indexOf(item)) }})
                            </span>
                        </div>
                    </div>
                </div>

                <template v-if="state.remarks">
                    <USeparator />
                    <div>
                        <p
                            class="text-xs font-semibold text-[var(--ui-text-muted)] uppercase tracking-wide mb-3"
                        >
                            Remarks
                        </p>
                        <p>{{ state.remarks }}</p>
                    </div>
                </template>
            </div>

            <div class="flex justify-end gap-2">
                <UButton
                    color="neutral"
                    variant="outline"
                    @click="showConfirm = false"
                >
                    Back
                </UButton>
                <UButton
                    color="primary"
                    :loading="submitting"
                    @click="confirmSubmit"
                >
                    Confirm & Submit
                </UButton>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import * as z from "zod";
import { computed, reactive, ref, watch, onMounted } from "vue";
import type { ItemOption } from "@/services/ErpNextService";
import { ErpNextService } from "@/services/ErpNextService";
import { useStockDataStore } from "@/stores/StockDataStore";
import { formatNumber } from "@/utils/FormatNumber";

const toast = useToast();
const emit = defineEmits<{
    onSubmit: [
        payload: { items: { item_code: string; qty: number }[]; remarks?: string },
    ];
}>();

const props = defineProps<{ loading?: boolean }>();
const submitting = computed(() => props.loading ?? false);
const erpnext = new ErpNextService();
const stockStore = useStockDataStore();
const showConfirm = ref(false);

interface ReconItem {
    item_code: string;
    item_name: string;
    current_qty: number;
    corrected_qty: number;
}

const state = reactive({
    remarks: "",
    items: [
        { item_code: "", item_name: "", current_qty: 0, corrected_qty: 0 },
    ] as ReconItem[],
});

const schema = z.object({
    remarks: z.string().optional(),
    items: z
        .array(
            z.object({
                item_code: z.string().min(1, "Item is required"),
                item_name: z.string().optional(),
                current_qty: z.number(),
                corrected_qty: z.number(),
            }),
        )
        .min(1, "At least one item required"),
});

const validItems = computed(() => state.items.filter((i) => i.item_code));

const itemSelections = ref<(ItemOption | null)[]>([null]);
const itemOptions = ref<ItemOption[][]>([[]]);
const itemSearchTerms = ref<string[]>([""]);
const itemTimers: Record<number, ReturnType<typeof setTimeout>> = {};

function formatDiff(idx: number) {
    const item = state.items[idx];
    if (!item) return "";
    const diff = item.corrected_qty - item.current_qty;
    const sign = diff >= 0 ? "+" : "";
    return `${sign}${formatNumber(diff, "decimal")}`;
}

function diffClass(idx: number) {
    const item = state.items[idx];
    if (!item) return "";
    const diff = item.corrected_qty - item.current_qty;
    if (diff > 0) return "text-green-500";
    if (diff < 0) return "text-red-500";
    return "text-[var(--ui-text-muted)]";
}

function watchRow(idx: number) {
    watch(
        () => itemSearchTerms.value[idx],
        (term) => {
            if (itemTimers[idx]) clearTimeout(itemTimers[idx]);
            itemTimers[idx] = setTimeout(async () => {
                try {
                    const results = await erpnext.searchItems(term || "");
                    if (results) itemOptions.value[idx] = results;
                } catch {
                    /* ignore */
                }
            }, 300);
        },
    );
}

watchRow(0);

onMounted(async () => {
    try {
        const items = await erpnext.searchItems("");
        if (items) itemOptions.value[0] = items;
    } catch {
        /* ignore */
    }
});

async function onItemOpen(idx: number) {
    if (!itemOptions.value[idx]?.length) {
        try {
            const results = await erpnext.searchItems("");
            if (results) itemOptions.value[idx] = results;
        } catch {
            /* ignore */
        }
    }
}

function onItemPicked(idx: number) {
    const sel = itemSelections.value[idx];
    if (!sel) return;
    const target = state.items[idx];
    if (!target) return;
    target.item_code = sel.item_code;
    target.item_name = sel.item_name;

    const stockRow = stockStore.stockTableData.find(
        (r) => r.item_code === sel.item_code,
    );
    if (stockRow) {
        target.current_qty = stockRow.real_qty;
        target.corrected_qty = stockRow.real_qty;
    }
}

function addItem() {
    state.items.push({
        item_code: "",
        item_name: "",
        current_qty: 0,
        corrected_qty: 0,
    });
    itemSelections.value.push(null);
    itemOptions.value.push([]);
    itemSearchTerms.value.push("");
    const newIdx = state.items.length - 1;
    watchRow(newIdx);
    onItemOpen(newIdx);
}

function removeItem(idx: number) {
    if (state.items.length > 1) {
        state.items.splice(idx, 1);
        itemSelections.value.splice(idx, 1);
        itemOptions.value.splice(idx, 1);
        itemSearchTerms.value.splice(idx, 1);
        if (itemTimers[idx]) {
            clearTimeout(itemTimers[idx]);
            delete itemTimers[idx];
        }
    }
}

function onFormSubmit() {
    if (validItems.value.length === 0) {
        toast.add({
            title: "Please add at least one item",
            color: "error",
        });
        return;
    }
    showConfirm.value = true;
}

function confirmSubmit() {
    emit("onSubmit", {
        items: validItems.value.map((i) => ({
            item_code: i.item_code,
            qty: i.corrected_qty,
        })),
        remarks: state.remarks || undefined,
    });
}
</script>
```

- [ ] **Step 2: Verify build**

Run: `npm run build` from `frontend/`
Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/StockReconciliationModal.vue
git commit -m "feat: add StockReconciliationModal component"
```

---

### Task 5: Wire modals and events in StockView

**Files:**
- Modify: `frontend/src/views/StockView.vue`

- [ ] **Step 1: Add imports**

After line 56 (`import PurchaseForm from "@/components/PurchaseForm.vue";`), add:

```ts
import StockReconciliationModal from "@/components/StockReconciliationModal.vue";
import DisableItemConfirmModal from "@/components/DisableItemConfirmModal.vue";
```

- [ ] **Step 2: Add new state and the reconciliation modal in template**

Replace the top-right modal section (lines 3-20) with two modals:

```html
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
```

- [ ] **Step 3: Update StockTable usage to wire the disable-item event**

Replace line 46-49:

```html
        <StockTable
            :data="stockDataStore.stockTableData"
            :loading="dataStore.loading"
        />
```

With:

```html
        <StockTable
            :data="stockDataStore.stockTableData"
            :loading="dataStore.loading"
            @disable-item="handleDisableItem"
        />
```

- [ ] **Step 4: Add the DisableItemConfirmModal at the end of the template**

After the StockTable (after line 49, before `</DashboardLayout>` at line 50), add:

```html
        <DisableItemConfirmModal
            v-if="disableTarget"
            v-model:open="openDisableConfirm"
            :item-code="disableTarget.item_code"
            :item-name="disableTarget.item_name"
            :current-qty="disableTarget.real_qty"
            @confirm="onDisableConfirm"
        />
```

- [ ] **Step 5: Add new refs and handlers in script setup**

After the existing refs (line 67-68):

```ts
const openPurchase = ref(false);
const isMobile = ref(false);
```

Add:

```ts
const openReconciliation = ref(false);
const reconciliationLoading = ref(false);
const openDisableConfirm = ref(false);
const disableTarget = ref<{ item_code: string; item_name: string; real_qty: number } | null>(null);
const selectedWarehouse = ref("Stores");
```

- [ ] **Step 6: Add warehouse fetch in onMounted**

Replace the existing `onMounted` block (lines 74-79):

```ts
onMounted(() => {
  const mq = window.matchMedia('(max-width: 767px)');
  syncMobile(mq);
  mq.addEventListener('change', syncMobile);
  onUnmounted(() => mq.removeEventListener('change', syncMobile));
});
```

With:

```ts
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
```

- [ ] **Step 7: Add handler functions**

After the `onPurchaseSubmit` function (after line 131), add:

```ts
function handleDisableItem(row: { item_code: string; item_name: string; real_qty: number }) {
  disableTarget.value = row;
  openDisableConfirm.value = true;
}

async function onReconcileSubmit(payload: {
  items: { item_code: string; qty: number }[];
  remarks?: string;
}) {
  reconciliationLoading.value = true;
  const result = await erpnext.createStockReconciliation({
    warehouse: selectedWarehouse.value,
    items: payload.items,
    company: authStore.company || "",
    remarks: payload.remarks,
  });
  reconciliationLoading.value = false;

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
}

async function onDisableConfirm(payload: { remarks?: string }) {
  if (!disableTarget.value) return;
  openDisableConfirm.value = false;
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
}
```

- [ ] **Step 8: Verify build**

Run: `npm run build` from `frontend/`
Expected: No errors.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/views/StockView.vue
git commit -m "feat: wire reconciliation modal and disable-item flow in StockView"
```

