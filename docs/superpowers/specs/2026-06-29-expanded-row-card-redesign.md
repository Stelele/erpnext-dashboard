# Expanded Row Card Redesign

**Date**: 2026-06-29
**Status**: Approved
**Component**: `frontend/src/components/ExpenseTable.vue`

## Problem

On mobile, the expanded row requires a second tap (View Order Details) to see line items. Users see everything except line items — the button just adds friction. Modal-on-expanded-row is two layers deep on small screens.

## Design Decisions

1. **Keep expand chevron + row tap** — the friction complaint was about the "View Order Details" button, not the expand interaction. The chevron column and `onRowSelect` handler stay as-is.
2. **Soft card layout** — expanded content rendered as a raised card with 3 sections separated by dividers.
3. **Embed line items inline** — no modal, no second click. Line items go directly in the card.
4. **Lazy-fetch on expand** — `getPurchaseInvoice(id)` called when row expands, cached in a reactive Map. Loading skeleton shown while fetching.
5. **Supplier for Orders** — the "Description" label becomes "Supplier" for Order-type rows, sourcing from the purchase invoice response.

## Template Structure

### Expanded slot (`#expanded`) — 3 sections

```
┌─ Card (bg-elevated, rounded-lg, border, shadow-sm) ─────────────┐
│                                                                  │
│  Section 1: Summary                                              │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ Date: 15 Jun 2026     #: ACC-PINV-2026-00042             │   │
│  │ Status: [Submitted]   Type: Order                        │   │
│  │ Supplier: ABC Ltd. | Amount: $12,450.00                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ─── divider (border-t border-(--ui-border)) ────────────────   │
│                                                                  │
│  Section 2: Line Items (Orders only, hidden for Expenses)        │
│  Header: "Line Items · N"                                        │
│  ┌─ loading shimmer (while fetching) ──────────────────────┐    │
│  │ or                                                       │    │
│  │ Item              Qty     Total                           │    │
│  │ Rice 25kg         10      $450                            │    │
│  │ Cooking Oil 20L    5      $300                            │    │
│  │ Sugar 50kg         8      $440                            │    │
│  │ ▸ max-h-36 overflow-y-auto for 7+ items                  │    │
│  └──────────────────────────────────────────────────────────┘   │
│  ─── divider (border-t border-(--ui-border)) ────────────────   │
│                                                                  │
│  Section 3: Actions                                              │
│  ┌─ [Cancel] [Amend] (only when status === "Submitted") ───┘    │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Conditional field labeling

| Type | Label | Value source |
|------|-------|-------------|
| Order | Supplier | `purchaseInvoice.supplier` (fetched) |
| Expense | Description | `row.original.description` (in Payment) |

## Data Flow

```
Row expands
  → expanded ref updates (v-model:expanded)
  → Watch/effect detects row is expanded
  → If type === "Order":
      → Check invoicesCache Map
      → Cache miss: call erpnext.getPurchaseInvoice(row.original.id)
      → Show loading skeleton in section 2
      → Cache hit or response: populate supplier + line items
  → If type === "Expense":
      → Skip section 2 entirely
      → Show "Description" from Payment
```

### Cache

```ts
// Reactive cache keyed by purchase invoice ID
const purchaseInvoices = ref<Map<string, PurchaseInvoiceResponse>>(new Map());
```

Cache is not cleared (session-lifetime). Typical usage has 10-30 rows, negligible memory.

## Component Changes

### Add
- `ref<Map<string, PurchaseInvoiceResponse>>` for cache
- Loading state per-row (can derive from cache key existence)
- Logic to fetch on expand (watch/reactive effect or method called from template)
- Loading shimmer JSX/slot in line items section

### Remove
- `import OrderDetailsModal` (line 116)
- `<OrderDetailsModal>` usage (lines 88-91)
- `"View Order Details"` button in expanded template (lines 58-66)
- `selectedOrderId` ref (line 118) — no longer needed

### Keep (unchanged)
- Expand chevron column definition (lines 130-153)
- `onRowSelect` handler (lines 122-127)
- All other columns (id, date, status, type, description, amount, actions)
- Props (`data`, `loading`) and emits (`cancel`, `amend`)

### Modify
- `#expanded` template (lines 25-86) — replaced with card layout
- Conditional label: `<div>{{ row.original.type === 'Order' ? 'Supplier' : 'Description' }}</div>`

## Nuxt UI Components Needed

Already available in component scope:
- `UBadge` (resolved via resolveComponent)
- `UButton` (resolved via resolveComponent)

New from Nuxt UI:
- `USkeleton` for loading shimmer in line items section (if available, else custom CSS shimmer)

## Edge Cases

| Case | Behavior |
|------|----------|
| Fetch fails | Show "Failed to load line items" with retry button |
| Fetch slow (3s+) | Loading skeleton visible, no timeout — wait |
| Order with 0 items | Show "No line items" in section 2 |
| Order with 20+ items | `max-h-36` caps visible area, scrollable |
| Expense type row expanded | Only sections 1 + 3, no section 2 |
| Row collapsed while fetching | Request continues (don't cancel), result cached for next expand |
| Network offline | Show error state, no crash |
| Rapid expand/collapse | Cache prevents duplicate requests |

## Testing

1. **Unit**: Verify cache map works (hit, miss, populate)
2. **Integration**: Expand an Order row → loading shimmer → line items appear
3. **Integration**: Expand an Expense row → no line items section, "Description" shown
4. **Integration**: Expand Order row → "Supplier" label shown instead of "Description"
5. **Mobile**: Verify at 412px width (Samsung A50) — no horizontal overflow, scroll works
6. **Edge**: Order with 20+ items → scrollable container, not infinite height
7. **Edge**: API failure → error state with retry
8. **Regression**: Cancel/Amend buttons still work
9. **Regression**: Desktop table columns unchanged, actions column still visible
