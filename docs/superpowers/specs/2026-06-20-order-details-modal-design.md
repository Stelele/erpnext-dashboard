# Order Details Modal

**Date:** 2026-06-20
**Status:** Approved

## Overview

Add a "View Order Details" button to the Expenses table that opens a modal showing line-item breakdown for a given purchase invoice — item name, quantity, rate, and line total — along with header-level summary (supplier, invoice #, date, grand total).

## Motivation

The Expenses table currently shows Order rows with only supplier name and grand total. Users need to see what was actually ordered and at what rates without leaving the dashboard.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Button placement | Both Actions column (ℹ️ icon) + expanded row button | Maximum accessibility |
| Modal component | Separate `OrderDetailsModal.vue` | Follows existing pattern (CreateSupplierModal, CreateItemModal), keeps ExpenseTable focused |
| Data source | ERPNext REST API on-demand | Fetch `/api/resource/Purchase Invoice/{id}` when modal opens — no new Python scripts |
| Item columns | Item name, quantity, rate, line total | Covers the key questions: what, how many, at what price |
| Modal header | Supplier, invoice #, date, grand total | Provides context before the line items |
| Modal actions | Read-only | Viewing only; cancel action already exists on the table row |

## Architecture

### Files

| File | Change |
|------|--------|
| `frontend/src/components/OrderDetailsModal.vue` | **NEW** — modal with header summary + items table |
| `frontend/src/types/Expenses.ts` | Add `PurchaseInvoiceItem` and `PurchaseInvoiceResponse` interfaces |
| `frontend/src/services/ErpNextService.ts` | Add `getPurchaseInvoice(name: string)` method |
| `frontend/src/components/ExpenseTable.vue` | Add buttons + import/mount modal |

### Data Flow

```
User clicks ℹ️ (or "View Order Details" in expanded row)
  → ExpenseTable sets selectedOrderId
    → OrderDetailsModal watches purchaseInvoiceId prop
      → calls ErpNextService.getPurchaseInvoice(id)
        → GET /api/resource/Purchase Invoice/{id}
          → returns { supplier, posting_date, grand_total, items: [...] }
            → renders header + items table
```

### Component Tree

```
ExpenseTable.vue
  ├── UTable (existing)
  │     └── expanded template → "View Order Details" button (Order rows only)
  │     └── Actions column → ℹ️ icon button (Order rows only, next to Cancel)
  └── OrderDetailsModal.vue
        ├── UModal
        │     ├── Header summary (UCard): supplier, invoice #, date, total
        │     └── UTable: item name, quantity, rate, line total
        └── Loading state / error toast
```

## Types

```typescript
// New in Expenses.ts
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

## OrderDetailsModal Component Spec

### Props

| Prop | Type | Description |
|------|------|-------------|
| `purchaseInvoiceId` | `string \| null` | Purchase Invoice name. Modal opens when non-null, closes when null. |

### Emits

| Event | Payload | Description |
|-------|---------|-------------|
| `close` | none | User closes the modal |

### States

- **Closed**: `purchaseInvoiceId` is null, modal is hidden
- **Loading**: Fetch in progress, show spinner
- **Loaded**: Display header summary + items table
- **Error**: API call failed, show error toast, auto-close

### Behaviors

- Resets state when `purchaseInvoiceId` changes (new fetch on each open)
- Clears data on close

## ErpNextService Changes

```typescript
public async getPurchaseInvoice(name: string): Promise<PurchaseInvoiceResponse> {
  return this.instance
    .get<PurchaseInvoiceResponse>(
      `/api/resource/Purchase Invoice/${encodeURIComponent(name)}`
    )
    .then((resp) => resp.data);
}
```

## ExpenseTable Changes

1. Add `selectedOrderId: ref<string | null>(null)` state
2. Add ℹ️ button in Actions column for Order rows (alongside Cancel button)
3. Add "View Order Details" button in expanded row template for Order rows
4. Import and render `<OrderDetailsModal :purchase-invoice-id="selectedOrderId" @close="selectedOrderId = null" />`

## Edge Cases

- **Draft orders**: Info button still shown (user may want to see items before submitting)
- **Cancelled orders**: Info button shown (items are historical/reference)
- **Missing items array**: ERPNext may return Purchase Invoices with empty items — handle gracefully with "No items found" message
- **Deleted purchase invoice**: API returns 404 — show error toast
