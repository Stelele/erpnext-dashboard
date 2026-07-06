# Stock Reconciliation & Item Disable — Design Spec

**Date:** 2026-07-06
**Approach:** A — StockView-hosted modals + StockTable emits events

## Overview

Two new features on the Stock page (`/stock`):

1. **Stock Reconciliation Modal** — creates an ERPNext Stock Reconciliation entry to correct item quantities
2. **Disable Item Button** — zeroes an item's stock via reconciliation, then disables the item in ERPNext

---

## Feature 1: Stock Reconciliation Modal

### Component: `StockReconciliationModal.vue`

**Location:** Hosted in `StockView.vue`, following the same pattern as the Quick Purchase modal

**Trigger:** "Reconcile Stock" button in the top-right of StockView, next to "Quick Purchase"

### Warehouse

Auto-selected on mount (same pattern as `PurchaseForm.vue` lines 369-377):
- Fetches warehouses via `erpnext.getWarehouses()`
- Picks the one with "stores" in the name
- Falls back to the first warehouse
- Not shown as a user-facing field

### Form Fields (per line item)

| Field | Type | Behavior |
|---|---|---|
| Item | Searchable dropdown (`UInputMenu`) | Same pattern as PurchaseForm item rows. Searches ERPNext items. |
| Current Qty | Read-only number | Auto-filled after item is picked, fetched from current stock data |
| Corrected Qty | Number input | Defaults to current qty. User overrides with actual count. |
| Difference | Computed read-only | `corrected_qty - current_qty`. Color-coded: green (pos), red (neg), gray (zero). |

### Global Fields

| Field | Type | Required |
|---|---|---|
| Remarks | Textarea | No (optional, applied to the ERPNext reconciliation doc) |

### Chrome

- "Add Item" button appends a new row (same pattern as PurchaseForm)
- Remove button (x) on each row, hidden when only 1 row exists
- Uses Zod schema validation
- 2-step confirmation: form → review summary → submit (same as PurchaseForm/ExpenseForm)
- Toast notification on success/failure
- On success: closes modal, calls `dataStore.update()` to refresh all data

### ERPNext API Call

`POST /api/resource/Stock Reconciliation` — standard Frappe REST API via `ErpNextService.createStockReconciliation()`

---

## Feature 2: Disable Item Button

### Placement

**In `StockTable.vue`:**
- New `actions` column (rightmost column, hidden on mobile via the existing column class pattern)
- Button: small error/red icon button (e.g., `i-lucide-ban` or similar), with tooltip "Disable Item"

**Also in expanded row detail:**
- "Disable Item" button in the expanded detail grid — gives extra context (user can see item name, group, current qty before clicking)

### Flow

1. User clicks disable button → `StockTable` emits `@disable-item({ item_code, item_name, real_qty })`
2. `StockView` receives the event and opens `DisableItemConfirmModal.vue`
3. Confirm modal shows:
   - Item name and current quantity (read-only)
   - Optional remarks field (noted on the zero-out reconciliation entry)
   - Warning: "This will zero out stock via a reconciliation and disable the item. This cannot be undone."
   - Cancel / "Disable Item" buttons
4. On confirm: calls `erpnext.disableItem(itemCode, qty, remarks)`
5. On success: calls `dataStore.update()` → item disappears on next refresh (qty=0, filtered by ERPNext)

### ERPNext API Calls (sequenced in `ErpNextService.disableItem()`)

1. `POST /api/resource/Stock Reconciliation` — 1 line item with `qty=0` to zero out stock
2. `PUT /api/resource/Item/{item_code}` — sets `disabled: 1`

Both calls are sequential. If the reconciliation succeeds but the item disable fails, the user is notified via error toast. No rollback attempted (requires a custom ERPNext endpoint for transactional safety).

---

## Service Layer Changes

### New methods in `ErpNextService.ts`

```ts
createStockReconciliation(payload: {
  warehouse: string;
  items: { item_code: string; qty: number }[];
  company: string;
  remarks?: string;
}): Promise<boolean>

disableItem(itemCode: string, currentQty: number, remarks?: string): Promise<boolean>
```

- Follow the existing try/catch + boolean/null return pattern (matching `createFullPurchase`)
- Use the existing Axios instance with ERPNext auth token
- Error handling via existing centralized interceptors

---

## Component Changes Summary

| File | Change |
|---|---|
| `frontend/src/components/StockReconciliationModal.vue` | **New** — reconciliation form modal |
| `frontend/src/components/DisableItemConfirmModal.vue` | **New** — confirmation dialog for disable |
| `frontend/src/services/ErpNextService.ts` | **Modified** — add `createStockReconciliation()` and `disableItem()` |
| `frontend/src/components/StockTable.vue` | **Modified** — add actions column, expanded row button, emit `disable-item` |
| `frontend/src/views/StockView.vue` | **Modified** — host both modals, wire events, handle submissions |

---

## Error Handling

- All API calls go through `ErpNextService`'s existing try/catch pattern
- Modals show toast notifications on success (`color: "success"`) and failure (`color: "error"`)
- Form validation via Zod schema prevents invalid submissions before API call
- Disable item's two-step nature: reconciliation success + item disable failure = error toast, no rollback
