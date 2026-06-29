# Expense Table Expanded Row Card Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the modal-based "View Order Details" flow with inline line items and a soft card layout in the expanded table row.

**Architecture:** Single-component change to `ExpenseTable.vue`. Purchase invoice data is lazy-fetched via `ErpNextService.getPurchaseInvoice()` on first expand of an Order row, cached in a reactive `Map` for the session. Expense-type rows skip line items. The `OrderDetailsModal` import and usage are removed entirely.

**Tech Stack:** Vue 3, Nuxt UI v4 (UBadge, UButton, UTable), TanStack Table, TypeScript

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `frontend/src/components/ExpenseTable.vue` | **Modify** | Expanded row template, fetch logic, cache, clean up old modal |

No files created or deleted. `OrderDetailsModal.vue` remains in the codebase (may be used elsewhere).

---

### Task 1: Add purchase invoice fetching infrastructure

**Files:**
- Modify: `frontend/src/components/ExpenseTable.vue` (script section)

- [ ] **Step 1: Add ErpNextService import and PurchaseInvoiceResponse type**

In the `<script setup>` imports block (after line 99), add:

```ts
import { ErpNextService } from "@/services/ErpNextService";
import type { PurchaseInvoiceResponse } from "@/types/Expenses";
```

- [ ] **Step 2: Add reactive state for cache, loading, and errors**

After the `emit` definition (after line 114), add:

```ts
const erpnext = new ErpNextService();

const purchaseInvoices = ref<Map<string, PurchaseInvoiceResponse["data"]>>(new Map());
const invoiceLoading = ref<Set<string>>(new Set());
const invoiceError = ref<Set<string>>(new Set());
```

- [ ] **Step 3: Add fetch and accessor functions**

After the `expanded` ref declaration (after line 120), add:

```ts
function ensureInvoice(id: string) {
  if (purchaseInvoices.value.has(id) || invoiceLoading.value.has(id)) return;
  fetchInvoice(id);
}

function fetchInvoice(id: string) {
  invoiceError.value.delete(id);
  if (invoiceLoading.value.has(id)) return;
  invoiceLoading.value.add(id);
  erpnext.getPurchaseInvoice(id).then((resp) => {
    invoiceLoading.value.delete(id);
    if (resp?.data) {
      purchaseInvoices.value.set(id, resp.data);
    } else {
      invoiceError.value.add(id);
    }
  });
}

function invoiceFor(id: string): PurchaseInvoiceResponse["data"] | undefined {
  return purchaseInvoices.value.get(id);
}

function isInvoiceLoading(id: string): boolean {
  return invoiceLoading.value.has(id);
}

function hasInvoiceError(id: string): boolean {
  return invoiceError.value.has(id);
}
```

- [ ] **Step 4: Build frontend to verify no TypeScript errors**

```bash
npm run build
```

---

### Task 2: Redesign expanded template with soft card layout

**Files:**
- Modify: `frontend/src/components/ExpenseTable.vue` (lines 25-86, the `#expanded` template)

- [ ] **Step 1: Replace the expanded template**

Replace lines 25-86 (the entire `#expanded` slot) with:

```html
                <template #expanded="{ row }">
                    <div class="mx-2 mb-2 rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated)/60 p-3 shadow-sm">
                        <!-- Trigger invoice fetch for Order rows -->
                        <template v-if="row.original.type === 'Order'">
                            {{ ensureInvoice(row.original.id) }}
                        </template>

                        <!-- Section 1: Summary -->
                        <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                            <div class="text-(--ui-text-muted)">Date</div>
                            <div>{{ moment(row.original.date).format("DD MMM YYYY") }}</div>
                            <div class="text-(--ui-text-muted)">#</div>
                            <div class="truncate">{{ row.original.id }}</div>
                            <div class="text-(--ui-text-muted)">Status</div>
                            <div>
                                <UBadge class="capitalize" variant="subtle" :color="getStatusColor(row.original.status)">
                                    {{ row.original.status }}
                                </UBadge>
                            </div>
                            <div class="text-(--ui-text-muted)">Type</div>
                            <div>{{ row.original.type }}</div>
                            <div class="text-(--ui-text-muted)">
                                {{ row.original.type === 'Order' ? 'Supplier' : 'Description' }}
                            </div>
                            <div class="text-wrap break-words">
                                {{ row.original.type === 'Order' ? (invoiceFor(row.original.id)?.supplier ?? '—') : row.original.description }}
                            </div>
                            <div class="text-(--ui-text-muted)">Amount</div>
                            <div class="font-medium">{{ formatNumber(row.original.amount, "currency") }}</div>
                        </div>

                        <!-- Section 2: Line Items (Order type only) -->
                        <template v-if="row.original.type === 'Order'">
                            <div class="my-3 border-t border-(--ui-border)"></div>
                            <div class="flex items-center gap-2 mb-2">
                                <span class="text-xs text-(--ui-text-muted) uppercase tracking-wider">Line Items</span>
                                <span class="text-xs text-(--ui-text-muted)">
                                    &middot; {{ invoiceFor(row.original.id)?.items?.length ?? 0 }}
                                </span>
                            </div>

                            <!-- Loading skeleton -->
                            <div v-if="isInvoiceLoading(row.original.id)" class="animate-pulse space-y-2 py-1">
                                <div class="h-3 bg-(--ui-border) rounded w-full"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-3/4"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-5/6"></div>
                                <div class="h-3 bg-(--ui-border) rounded w-1/2"></div>
                            </div>

                            <!-- Error state -->
                            <div v-else-if="hasInvoiceError(row.original.id)" class="flex items-center gap-2 py-1 text-sm text-(--ui-error)">
                                <span class="i-lucide-alert-circle size-4 inline-block"></span>
                                <span>Failed to load line items</span>
                                <UButton size="xs" variant="ghost" color="error" @click="fetchInvoice(row.original.id)">
                                    Retry
                                </UButton>
                            </div>

                            <!-- Empty items -->
                            <div v-else-if="!invoiceFor(row.original.id)?.items?.length" class="py-1 text-xs text-(--ui-text-muted)">
                                No line items
                            </div>

                            <!-- Line items table -->
                            <div v-else class="border border-(--ui-border) rounded-md overflow-hidden max-h-36 overflow-y-auto">
                                <div class="grid grid-cols-[1fr_auto_auto] gap-x-3 px-3 py-1.5 bg-(--ui-bg-elevated) text-xs text-(--ui-text-muted) uppercase sticky top-0">
                                    <div>Item</div>
                                    <div>Qty</div>
                                    <div class="text-right">Total</div>
                                </div>
                                <div
                                    v-for="item in invoiceFor(row.original.id)!.items"
                                    :key="item.item_code"
                                    class="grid grid-cols-[1fr_auto_auto] gap-x-3 px-3 py-1.5 text-sm border-t border-(--ui-border)"
                                >
                                    <div class="truncate">{{ item.item_name }}</div>
                                    <div class="tabular-nums">{{ item.qty }}</div>
                                    <div class="text-right tabular-nums font-medium">{{ formatNumber(item.amount, "currency") }}</div>
                                </div>
                            </div>
                        </template>

                        <!-- Section 3: Actions -->
                        <div
                            v-if="row.original.status === 'Submitted'"
                            class="mt-3 pt-3 border-t border-(--ui-border) flex gap-2"
                        >
                            <UButton
                                color="error"
                                variant="ghost"
                                icon="i-lucide-x"
                                @click="emit('cancel', row.original)"
                            >
                                Cancel
                            </UButton>
                            <UButton
                                color="success"
                                variant="ghost"
                                icon="i-lucide-pencil"
                                @click="emit('amend', row.original)"
                            >
                                Amend
                            </UButton>
                        </div>
                    </div>
                </template>
```

- [ ] **Step 2: Build to verify no errors**

```bash
npm run build
```

---

### Task 3: Remove OrderDetailsModal usage

**Files:**
- Modify: `frontend/src/components/ExpenseTable.vue`

- [ ] **Step 1: Remove OrderDetailsModal import**

Remove line 116:
```ts
import OrderDetailsModal from "@/components/OrderDetailsModal.vue";
```

- [ ] **Step 2: Remove selectedOrderId ref**

Remove line 118:
```ts
const selectedOrderId = ref<string | null>(null);
```

- [ ] **Step 3: Remove OrderDetailsModal from template**

Remove lines 88-91:
```html
            <OrderDetailsModal
                :purchase-invoice-id="selectedOrderId"
                @close="selectedOrderId = null"
            />
```

- [ ] **Step 4: Build and check for unreferenced symbols**

```bash
npm run build
```

Expected: clean build, no lint/type errors.

---

### Task 4: Verify the complete implementation

**Files:**
- Verify: `frontend/src/components/ExpenseTable.vue`
- Verify: `frontend/src/components/OrderDetailsModal.vue` (untouched, still exists)

- [ ] **Step 1: Run type check**

```bash
npm run typecheck
```

Expected: zero errors.

- [ ] **Step 2: Run lint**

```bash
npm run lint
```

Expected: zero errors.

- [ ] **Step 3: Final build verification**

```bash
npm run build
```

Expected: `✓ built in X.XXs`

- [ ] **Step 4: Manual code review checklist**

Verify in the final source:
- [x] No `OrderDetailsModal` import in `ExpenseTable.vue`
- [x] No `selectedOrderId` ref
- [x] No `<OrderDetailsModal>` in template
- [x] `ensureInvoice()` called in expanded template for Order rows
- [x] `#expanded` template has card wrapper with 3 sections
- [x] "Supplier" label for Orders, "Description" for Expenses
- [x] Loading skeleton in line items section
- [x] Error state with Retry button
- [x] `max-h-36 overflow-y-auto` on line items container
- [x] Cancel/Amend buttons still present conditionally
- [x] Chevron expand column and `onRowSelect` unchanged
- [x] Desktop columns (id, status, description, actions) unchanged
