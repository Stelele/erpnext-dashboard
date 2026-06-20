# Amend Orders & Expenses — Design Spec

**Date**: 2026-06-20  
**Status**: Draft

## Overview

Add an "Amend" action to submitted expenses (Journal Entries) and orders (Purchase Invoices). Following ERPNext semantics: cancel the original document, create a new corrected one linked via `amended_from`.

## Approach

**Single atomic server-side Python scripts** — cancel-then-create in one `try/rollback` block. No sequential client-side calls. Network failure = nothing happened, user retries.

---

## 1. Architecture & Data Flow

### Expense Amend
```
User clicks Amend on an Expense row
  → Fetch original Journal Entry from ERPNext
  → Extract pre-fill values: amount, description, expense type, date
  → Open ExpenseForm in amend mode (pre-filled)
  → User edits + submits
  → Single call: POST /api/v2/method/amend_expense_journal_entry
      Server-side:
        1. Cancel old JE (docstatus=2)
        2. Create new JE with amended_from={original_id}
        3. Submit new JE (docstatus=1)
      → All in one transaction with rollback on failure
  → Returns new Journal Entry name
  → Refresh table
```

### Order Amend
```
User clicks Amend on an Order row
  → Fetch original Purchase Invoice from ERPNext
  → Extract pre-fill values: supplier, items, invoice_number, invoice_date
    (date and warehouse locked/readonly in the form)
  → Open PurchaseForm in amend mode (pre-filled)
  → User edits items + submits
  → Single call: POST /api/v2/method/amend_full_purchase
      Server-side:
        1. Cancel old chain: PE → PI → PR → PO
        2. Create new chain: PO → PR → PI → PE with amended_from on PI
        3. Update item prices
      → All in one transaction with rollback on failure
  → Returns new purchase doc names
  → Refresh table
```

### Cancelled Visibility
- `dashboard_payment_entries.py`: remove `docstatus < 2` filter from both UNION parts
- Cancelled entries appear in the table with status "Cancelled" and no action buttons

---

## 2. UI Changes

### ExpenseTable.vue
- **Amend button** next to Cancel in both desktop action column and expanded mobile row
- Only visible when `status === 'Submitted'`
- Style: ghost button with green border, matching Cancel's error style
- Emits `amend` event with the Payment object
- **Loading state**: spinner + disabled during amend operation

### ExpenseForm.vue
- New optional prop: `amendEntry?: { id: string; amount: number; description: string; expenseTypeId: number; date: string }`
- When set: title changes to "Amend Expense", fields pre-populated, submit label becomes "Amend Entry"
- Emits new `amend` event (distinct from `submit`)
- Editable fields: amount, description, expense type

### PurchaseForm.vue
- New optional prop: `amendOrder?: { id: string; supplier: Supplier; items: Item[]; invoiceNumber: string; invoiceDate: string }`
- When set: title changes to "Amend Order", all fields pre-populated
- **Date + Warehouse fields are readonly** (disabled)
- Editable fields: supplier, items (add/remove/change qty), invoice_number, invoice_date
- Emits `amend` event

### ExpensesView.vue
- New handler `onAmendExpense(payment)`:
  1. Fetch original JE from ERPNext
  2. Map expense account back to expenseTypeId
  3. Open ExpenseForm modal with `amendEntry` prop
  4. On amend submit: call `amendExpenseJournalEntry`, refresh data

### StockView.vue
- New handler `onAmendPurchase(payment)`:
  1. Fetch original PI from ERPNext
  2. Extract supplier, items, invoice details
  3. Open PurchaseForm modal with `amendOrder` prop
  4. On amend submit: call `amendFullPurchase`, refresh data

### Mobile-specific
- Amend button inside `#expanded` template (same as Cancel)
- Same `onRowSelect` logic prevents expansion on button tap

---

## 3. Service Layer (ErpNextService.ts)

### New Methods

#### `amendExpenseJournalEntry(originalId, newExpense, incomeAccount, expenseAccount)`
```
POST /api/v2/method/amend_expense_journal_entry
Body: { journal_entry, amount, description, expense_account, income_account, posting_date, company, amended_from }
Returns: { journal_entry: string }
```

#### `amendFullPurchase(originalId, payload)`
```
POST /api/v2/method/amend_full_purchase
Body: { purchase_invoice, company, supplier, warehouse, items, invoice_number, invoice_date, amended_from }
Returns: { purchase_order, purchase_receipt, purchase_invoice, payment_entry }
```

#### `getJournalEntry(name)` — new helper
```
GET /api/resource/Journal Entry/{name}
Returns: JournalEntry | undefined
```

### Updated Types
- `Expense` interface: add `amendEntryId?: string`
- `PurchasePayload` type: add `amended_from?: string`

---

## 4. ERPNext Python Scripts

### New: `amend_expense_journal_entry.py`
```python
# Inputs: journal_entry (str), amount (float), description (str),
#          expense_account (str), income_account (str),
#          posting_date (str), company (str), amended_from (str)
#
# 1. Validate all inputs
# 2. Cancel old JE (docstatus=2)
# 3. Create new JE with amended_from, same accounts structure
# 4. Submit new JE (docstatus=1)
# 5. Return { journal_entry: new_name }
# All in try/rollback
```

### New: `amend_full_purchase.py`
```python
# Inputs: purchase_invoice (str), company (str), supplier (str),
#          warehouse (str), items (list), invoice_number (str),
#          invoice_date (str), amended_from (str)
#
# 1. Validate all inputs
# 2. Cancel old chain: PE → PI → PR → PO (reuse cancel_full_purchase logic)
# 3. Create new chain: PO → PR → PI → PE (reuse create_full_purchase logic)
# 4. Set amended_from on new PI
# 5. Update item prices
# 6. Return { purchase_order, purchase_receipt, purchase_invoice, payment_entry }
# All in try/rollback
```

### Modified: `dashboard_payment_entries.py`
- Remove `docstatus < 2` from both UNION ALL parts (lines 21, 39)
- Cancelled entries now appear with status "Cancelled"

### Modified: `create_full_purchase.py`
- Accept optional `amended_from` parameter
- When present: skip duplicate `bill_no` check for the amended_from PI
  (`if existing and existing != amended_from`)

---

## 5. Error Handling

### Atomicity
All amend logic runs server-side in a single `try/rollback` block. If any step fails, the entire operation rolls back and the original documents remain untouched.

### Validation
- Server validates all inputs before starting (company exists, supplier exists, items valid, etc.)
- Validation errors returned to the client as ERPNext error messages

### Frontend Error Display
- Catch errors from the single amend API call
- Show toast with error message from ERPNext
- Keep the form open so user can retry after fixing issues

### Edge Cases
| Scenario | Handling |
|---|---|
| Amend a cancelled entry | Amend button hidden for non-Submitted status |
| Double-click Amend | Button shows spinner + disabled |
| Original PI already partially cancelled | cancel_full_purchase checks `docstatus == 1` before each cancel |
| Same invoice number on amend | `create_full_purchase` skips duplicate check for `amended_from` PI |
| Concurrent amend (two users) | ERPNext handles at DB level — second amend will fail validation |

---

## 6. Testing

### Server Scripts
- Test `amend_expense_journal_entry.py`:
  - Successful amend: old JE cancelled, new JE created with `amended_from`
  - Rollback on invalid account: old JE stays submitted
- Test `amend_full_purchase.py`:
  - Successful amend: old chain cancelled, new chain created
  - Rollback on invalid item: old chain untouched

### Frontend
- Amend button visibility: only on Submitted, not on Draft/Cancelled
- Pre-fill correctness: form shows original values
- Loading state: button shows spinner during operation
- Error state: toast on failure, form stays open

### Existing Tests
- `npm run build` must pass
- `dotnet build` must pass
- Cancel functionality must not regress (cancel button still works)

---

## 7. Files Changed

| File | Change |
|---|---|
| `frontend/src/components/ExpenseTable.vue` | Add Amend button to expanded template & action column |
| `frontend/src/components/ExpenseForm.vue` | Add `amendEntry` prop, amend mode, amend emit |
| `frontend/src/components/PurchaseForm.vue` | Add `amendOrder` prop, amend mode, readonly date/warehouse |
| `frontend/src/views/ExpensesView.vue` | Add `onAmendExpense` handler |
| `frontend/src/views/StockView.vue` | Add `onAmendPurchase` handler |
| `frontend/src/services/ErpNextService.ts` | Add `amendExpenseJournalEntry`, `amendFullPurchase`, `getJournalEntry` |
| `frontend/src/services/ExpenseServiceFunctions.ts` | Add `amendExpense` wrapper |
| `frontend/src/types/Expenses.ts` | Add `amendEntryId` to Expense |
| `erpnext/server_scripts/amend_expense_journal_entry.py` | **New** — atomic expense amend |
| `erpnext/server_scripts/amend_full_purchase.py` | **New** — atomic order amend |
| `erpnext/server_scripts/create_full_purchase.py` | Accept optional `amended_from`, skip duplicate check |
| `erpnext/optimize/dashboard_payment_entries.py` | Remove `docstatus < 2` filter |
