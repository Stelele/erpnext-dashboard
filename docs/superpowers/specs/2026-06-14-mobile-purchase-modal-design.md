# Mobile-Friendly Purchase Modal — Design Spec

**Date:** 2026-06-14  
**Status:** Approved  
**Target Device:** Samsung A50 (412px × 892px CSS viewport, portrait)

## Context

The purchase modal (`PurchaseForm.vue` inside `StockView.vue`'s `UModal`) uses a 6-column grid (`grid-cols-[2fr_1fr_1fr_1fr_100px_auto]`) for item rows. On mobile devices this grid is unreadable — inputs are too narrow, labels overlap, and horizontal scrolling is required.

The codebase already has a proven responsive pattern in `SalesTable.vue`: `hidden md:block` for desktop table, `md:hidden` with `UCard`-based cards for mobile.

## Design

### Responsive Split

Two separate render blocks within `PurchaseForm.vue`, keyed to the `md:` breakpoint (768px):

- **Desktop (`hidden md:block`)**: Keep the current flat 6-column grid and 2-column header grid. No changes.
- **Mobile (`md:hidden`)**: All-new stacked layout using `UCard`, `UFormField`, `UButton`, `UInputMenu`, `UInput`.

### Mobile Layout

#### Header Fields (single column, full width)

Each field takes the full 412px width, stacked vertically:

```
Supplier *         [🔍 Search supplier ▾]
Warehouse *        [🔍 Stores ▾]
Invoice No.        [INV-2026-06-14-00123]
Invoice Date       [📅 14 Jun 2026]
```

All inputs use `UInputMenu` (searchable dropdowns for supplier/warehouse), `UInput` (for invoice number), and `UPopover` + `UCalendar` (for date).

#### Item Cards

Each item renders as a `UCard` with three zones:

```
┌─────────────────────────────────────┐
│  Steel Rod 12mm                [×]  │  ← header: bold product name + UButton ghost danger
│  HSN: 7214                          │  ← UBadge/subtitle (from item description)
├─────────────────────────────────────┤
│  Qty        Buy Rate    Sell Rate   │
│  [  1  ]    [ 50.00 ]   [ 75.00 ]  │  ← 3-col grid of labeled number inputs
├─────────────────────────────────────┤
│                   Total Buy: 50.00  │  ← footer: computed qty × rate
└─────────────────────────────────────┘
```

- Product name via `UInputMenu` search result's `item_name`
- Remove button: `UButton` `color="error" variant="ghost" icon="i-lucide-x"`
- Qty/Rate/Sell inputs: `UInput type="number"` wrapped in `UFormField` with `size="xs"` labels
- Total Buy: computed display, right-aligned in card footer
- "Add Item": `UButton variant="outline" class="w-full" icon="i-lucide-plus"`

#### Footer

Unchanged from current design:

```
Total: 87.50              [Submit Purchase]
```

### Modal Behavior

- **Mobile**: `UModal` with `:fullscreen="useMobileLayout"` — takes full screen, dismissible only via explicit close/cancel
- **Desktop**: Current behavior (`:ui="{ content: 'sm:max-w-2xl' }"`)

The `useMobileLayout` computed value is determined by `window.matchMedia('(max-width: 767px)')`.

### Confirmation Screen

No changes needed. Already a single-column stacked layout that works on all screen sizes.

## Nuxt UI Components Used

| Component | Usage |
|-----------|-------|
| `UModal` | Fullscreen on mobile, constrained width on desktop |
| `UCard` | Item cards on mobile |
| `UForm` | Form wrapper with Zod validation |
| `UFormField` | Labeled input groups with `size="xs"` on mobile |
| `UInputMenu` | Supplier, Warehouse, Product searchable dropdowns |
| `UInput` | Invoice number, Qty, Buy Rate, Sell Rate |
| `UPopover` + `UCalendar` | Invoice date picker |
| `UButton` | Remove item, Add Item, Submit, Back, Confirm |
| `USeparator` | Divider in confirmation view |

## Touch & UX Considerations

- Auto-scroll new items into viewport when "Add Item" is tapped
- Touch targets ≥ 44px (Nuxt UI defaults meet this)
- Number inputs use `inputmode="decimal"` for numeric keyboard
- Loading state disables all inputs (existing behavior, unchanged)

## Non-Goals

- Remains a `UModal`, not a `UDrawer` or separate route
- Desktop experience unchanged
- No backend changes
- Purchase form logic (validation, submit, item search) unchanged
