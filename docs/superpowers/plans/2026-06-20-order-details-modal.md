# Order Details Modal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "View Order Details" modal to the Expenses table that shows purchase invoice line items (item name, qty, rate, line total) with header summary (supplier, invoice #, date, grand total).

**Architecture:** New `OrderDetailsModal.vue` component using Nuxt UI `UModal` + `UTable`. Data fetched on-demand from ERPNext REST API (`GET /api/resource/Purchase Invoice/{id}`) via a new `ErpNextService.getPurchaseInvoice()` method. Buttons added to `ExpenseTable.vue` (Actions column ℹ️ icon + expanded-row button).

**Tech Stack:** Vue 3, Nuxt UI v4, TypeScript, Axios (via ErpNextService), Zod, moment

---

### Task 1: Add Types

**Files:**
- Modify: `frontend/src/types/Expenses.ts`

- [ ] **Step 1: Add PurchaseInvoiceItem and PurchaseInvoiceResponse interfaces**

Append after the existing `AccountMappings` interface (after line 50):

```typescript
export interface PurchaseInvoiceItem {
  item_name: string;
  qty: number;
  rate: number;
  amount: number;
}

export interface PurchaseInvoiceResponse {
  data: {
    name: string;
    supplier: string;
    posting_date: string;
    grand_total: number;
    items: PurchaseInvoiceItem[];
  };
}
```

- [ ] **Step 2: Verify types compile**

Run: `npx vue-tsc --noEmit --project frontend/tsconfig.json 2>&1 | head -30`
Expected: No errors related to `PurchaseInvoiceItem` or `PurchaseInvoiceResponse` (unrelated pre-existing errors may appear — that's fine, they're not from this change).

- [ ] **Step 3: Commit**

```bash
git add frontend/src/types/Expenses.ts
git commit -m "feat: add PurchaseInvoiceItem and PurchaseInvoiceResponse types"
```

---

### Task 2: Add getPurchaseInvoice to ErpNextService

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts`

- [ ] **Step 1: Import the new types**

Add `PurchaseInvoiceResponse` to the type import from `@/types/Expenses` (line 10-15). Change:

```typescript
import type {
  Expense,
  CompanyExpenseMapping,
  AccountMappings,
  AccountResponse,
} from "@/types/Expenses";
```

To:

```typescript
import type {
  Expense,
  CompanyExpenseMapping,
  AccountMappings,
  AccountResponse,
  PurchaseInvoiceResponse,
} from "@/types/Expenses";
```

- [ ] **Step 2: Add getPurchaseInvoice method**

Add before the `private getDateGrouping` method (before line 466):

```typescript
  public getPurchaseInvoice(name: string) {
    return this.instance
      .get<PurchaseInvoiceResponse>(
        `/api/resource/Purchase Invoice/${encodeURIComponent(name)}`
      )
      .then((resp) => resp.data)
      .catch(() => undefined);
  }
```

- [ ] **Step 3: Verify TypeScript compiles**

Run: `npx vue-tsc --noEmit --project frontend/tsconfig.json 2>&1 | head -30`
Expected: No new errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add getPurchaseInvoice method to ErpNextService"
```

---

### Task 3: Create OrderDetailsModal Component

**Files:**
- Create: `frontend/src/components/OrderDetailsModal.vue`

- [ ] **Step 1: Create the component file**

Write `frontend/src/components/OrderDetailsModal.vue`:

```vue
<template>
  <UModal v-model:open="open" title="Order Details">
    <template #body>
      <div class="p-4">
        <div v-if="loading" class="flex justify-center py-8">
          <UIcon name="i-lucide-loader-2" class="animate-spin text-2xl text-primary" />
        </div>

        <template v-else-if="invoice">
          <div class="grid grid-cols-2 gap-x-6 gap-y-2 mb-6">
            <div class="text-sm text-muted">Supplier</div>
            <div class="text-sm font-medium">{{ invoice.supplier }}</div>
            <div class="text-sm text-muted">Invoice #</div>
            <div class="text-sm font-medium">{{ invoice.name }}</div>
            <div class="text-sm text-muted">Date</div>
            <div class="text-sm font-medium">{{ moment(invoice.posting_date).format("DD MMM YYYY") }}</div>
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
    try {
      const resp = await erpnext.getPurchaseInvoice(id);
      if (resp?.data) {
        invoice.value = resp.data;
      } else {
        toast.add({ title: "Error", description: "Failed to load order details", color: "error" });
        close();
      }
    } catch {
      toast.add({ title: "Error", description: "Failed to load order details", color: "error" });
      close();
    } finally {
      loading.value = false;
    }
  },
  { immediate: true }
);

function close() {
  emit("close");
}
</script>
```

- [ ] **Step 2: Verify TypeScript compiles**

Run: `npx vue-tsc --noEmit --project frontend/tsconfig.json 2>&1 | head -30`
Expected: No new errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/OrderDetailsModal.vue
git commit -m "feat: add OrderDetailsModal component"
```

---

### Task 4: Wire Buttons into ExpenseTable

**Files:**
- Modify: `frontend/src/components/ExpenseTable.vue`

- [ ] **Step 1: Add selectedOrderId state and modal import**

In the `<script setup>` section, after the existing `const emit = defineEmits...` (line 78), add:

```typescript
import OrderDetailsModal from "@/components/OrderDetailsModal.vue";

const selectedOrderId = ref<string | null>(null);
```

- [ ] **Step 2: Add ℹ️ button in the Actions column**

In the Actions column `cell` function (lines 172-187), add an info button for Order rows. Change the return to render both buttons for Orders. Replace the entire `cell` function in the Actions column with:

```typescript
        cell: ({ row }) => {
            const buttons = [];

            if (row.original.type === "Order") {
                buttons.push(
                    h(UButton, {
                        color: "neutral",
                        variant: "ghost",
                        icon: "i-lucide-info",
                        square: true,
                        "aria-label": "Order details",
                        onClick: () => {
                            selectedOrderId.value = row.original.id;
                        },
                    })
                );
            }

            if (row.original.status === "Submitted") {
                const label = row.original.type === "Order" ? "Cancel purchase" : "Cancel expense";
                buttons.push(
                    h(UButton, {
                        color: "error",
                        variant: "ghost",
                        icon: "i-lucide-x",
                        square: true,
                        "aria-label": label,
                        onClick: () => emit("cancel", row.original),
                    })
                );
            }

            if (buttons.length === 0) return "";
            return h("div", { style: { display: "flex", gap: "4px" } }, buttons);
        },
```

- [ ] **Step 3: Add "View Order Details" button in expanded row template**

In the `#expanded` template slot (lines 24-53), add a button after the closing `</div>` of the grid (after line 53). Add before the `</template>`:

```vue
                        <div
                            v-if="row.original.type === 'Order'"
                            class="mt-4 pt-3 border-t border-(--ui-border)"
                        >
                            <UButton
                                color="neutral"
                                variant="ghost"
                                icon="i-lucide-info"
                                @click="selectedOrderId = row.original.id"
                            >
                                View Order Details
                            </UButton>
                        </div>
```

The full expanded template becomes:

```vue
                <template #expanded="{ row }">
                    <div class="grid grid-cols-2 w-full md:w-1/2 px-1 md:px-4">
                        <div>Date</div>
                        <div>
                            {{
                                moment(row.original.date).format("DD MMM YYYY")
                            }}
                        </div>
                        <div>#</div>
                        <div>{{ row.original.id }}</div>
                        <div>Status</div>
                        <div>
                            <UBadge
                                class="capitalize"
                                variant="subtle"
                                :color="getStatusColor(row.original.status)"
                                >{{ row.original.status }}</UBadge
                            >
                        </div>
                        <div>Type</div>
                        <div>{{ row.original.type }}</div>
                        <div>Desciption</div>
                        <div class="text-wrap">
                            {{ row.original.description }}
                        </div>
                        <div>Amount</div>
                        <div>
                            {{ formatNumber(row.original.amount, "currency") }}
                        </div>
                    </div>
                    <div
                        v-if="row.original.type === 'Order'"
                        class="mt-4 pt-3 border-t border-(--ui-border)"
                    >
                        <UButton
                            color="neutral"
                            variant="ghost"
                            icon="i-lucide-info"
                            @click="selectedOrderId = row.original.id"
                        >
                            View Order Details
                        </UButton>
                    </div>
                </template>
```

- [ ] **Step 4: Add OrderDetailsModal to the template**

Add the modal component after the closing `</UTable>` tag (after line 55) and before the closing `</div>` (line 56):

```vue
            <OrderDetailsModal
                :purchase-invoice-id="selectedOrderId"
                @close="selectedOrderId = null"
            />
```

The full template after the UTable becomes:

```vue
            <OrderDetailsModal
                :purchase-invoice-id="selectedOrderId"
                @close="selectedOrderId = null"
            />
        </div>
```

- [ ] **Step 5: Verify TypeScript compiles**

Run: `npx vue-tsc --noEmit --project frontend/tsconfig.json 2>&1 | head -30`
Expected: No new errors.

- [ ] **Step 6: Verify build succeeds**

Run: `npm run build`
Expected: Build completes without errors.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/components/ExpenseTable.vue
git commit -m "feat: add order details button and modal to ExpenseTable"
```
