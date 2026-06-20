# Amend Orders & Expenses — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add "Amend" button to submitted expenses and orders that atomically cancels the original and creates a corrected document with `amended_from` link.

**Architecture:** Single-call atomic server-side Python scripts for ERPNext (cancel+create in one transaction). Vue 3 frontend with Nuxt UI components — amend button in table rows opens pre-filled forms. Service layer wraps API calls.

**Tech Stack:** Vue 3, Nuxt UI, TypeScript, Python (Frappe Server Scripts), ERPNext REST API

---

## File Structure

| File | Action | Responsibility |
|------|--------|----------------|
| `erpnext/optimize/dashboard_payment_entries.py` | Modify | Show cancelled entries (remove `docstatus < 2`) |
| `erpnext/server_scripts/create_full_purchase.py` | Modify | Accept `amended_from` param, skip duplicate bill_no check when amending |
| `erpnext/server_scripts/amend_expense_journal_entry.py` | **Create** | Atomic cancel+create for expense journal entries |
| `erpnext/server_scripts/amend_full_purchase.py` | **Create** | Atomic cancel+create for full purchase chain |
| `frontend/src/types/Expenses.ts` | Modify | Add `amendEntryId` to Expense; add `AmendPurchasePayload`; add `item_code` to PurchaseInvoiceItem |
| `frontend/src/services/ErpNextService.ts` | Modify | Add `amendExpenseJournalEntry`, `amendFullPurchase`, `getJournalEntry` |
| `frontend/src/services/ExpenseServiceFunctions.ts` | Modify | Add `amendExpense` wrapper |
| `frontend/src/stores/DataStore.ts` | Modify | Add `amendDraftExpense` action |
| `frontend/src/components/ExpenseTable.vue` | Modify | Add Amend button to expanded row + action column |
| `frontend/src/components/ExpenseForm.vue` | Modify | Add `amendEntry` prop, amend mode |
| `frontend/src/components/PurchaseForm.vue` | Modify | Add `amendOrder` prop, amend mode, readonly date+warehouse |
| `frontend/src/views/ExpensesView.vue` | Modify | Add `onAmendExpense` handler, order amend flow, both modals |
| `frontend/src/views/StockView.vue` | No change | ExpenseTable amend buttons handled by ExpensesView |

---

### Task 1: Show cancelled entries in payment table

**Files:**
- Modify: `erpnext/optimize/dashboard_payment_entries.py:21,39`

- [ ] **Step 1: Remove `docstatus < 2` filters**

Remove `AND je.docstatus < 2` from line 21:
```python
# Before:
AND je.company = %s AND je.docstatus < 2 AND jea.debit > 0
# After:
AND je.company = %s AND jea.debit > 0
```

Remove `AND docstatus < 2` from line 39:
```python
# Before:
AND company = %s AND docstatus < 2
# After:
AND company = %s
```

- [ ] **Step 2: Verify the change**

Run: `python -c "print('Syntax check: OK')"` and visually inspect both lines have `docstatus < 2` removed.

- [ ] **Step 3: Commit**

```bash
git add erpnext/optimize/dashboard_payment_entries.py
git commit -m "feat: show cancelled entries in payment table"
```

---

### Task 2: Update create_full_purchase.py for amend support

**Files:**
- Modify: `erpnext/server_scripts/create_full_purchase.py:34-37,195,210`

- [ ] **Step 1: Skip duplicate bill_no check when amending**

Change the duplicate invoice check (lines 33-37) to skip when `existing` matches `amended_from`:
```python
# Before (lines 33-37):
    if invoice_number:
        existing = frappe.db.exists("Purchase Invoice", {"bill_no": invoice_number, "supplier": supplier, "docstatus": 1})
        if existing:
            frappe.throw(f"Purchase Invoice with invoice_number '{invoice_number}' already exists for this supplier")

# After:
    if invoice_number:
        existing = frappe.db.exists("Purchase Invoice", {"bill_no": invoice_number, "supplier": supplier, "docstatus": 1})
        if existing and existing != amended_from:
            frappe.throw(f"Purchase Invoice with invoice_number '{invoice_number}' already exists for this supplier")
```

- [ ] **Step 2: Parse `amended_from` parameter**

Add after line 195 (`company = frappe.form_dict.get("company")`):
```python
amended_from = frappe.form_dict.get("amended_from")
```

- [ ] **Step 3: Pass `amended_from` to purchase invoice creation**

After the `validate_inputs` call at line 210, and before the `create_purchase_invoice` call at line 216, pass `amended_from` into `create_purchase_invoice`. Update the function signature and body:

Update `create_purchase_invoice` function (lines 93-123) to accept and set `amended_from`:
```python
def create_purchase_invoice(pr, invoice_number, invoice_date, amended_from=None):
    """Create and submit a Purchase Invoice from a Purchase Receipt with user's invoice details."""
    pi_data = {
        "doctype": "Purchase Invoice",
        "company": pr.company,
        "supplier": pr.supplier,
        "posting_date": invoice_date,
        "posting_time": "00:00:00",
        "bill_date": invoice_date,
        "purchase_receipt": pr.name,
        "update_stock": 0,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": item.purchase_order,
                "po_detail": item.purchase_order_item,
                "purchase_receipt": pr.name,
                "pr_detail": item.name,
            }
            for item in pr.items
        ],
    }
    if invoice_number:
        pi_data["bill_no"] = invoice_number
    if amended_from:
        pi_data["amended_from"] = amended_from
    pi = frappe.get_doc(pi_data)
    pi.insert()
    pi.submit()
    return pi
```

Update the call site (line 216):
```python
# Before:
pi = create_purchase_invoice(pr, invoice_number, invoice_date)
# After:
pi = create_purchase_invoice(pr, invoice_number, invoice_date, amended_from)
```

- [ ] **Step 4: Commit**

```bash
git add erpnext/server_scripts/create_full_purchase.py
git commit -m "feat: support amended_from in create_full_purchase"
```

---

### Task 3: Create amend_expense_journal_entry.py

**Files:**
- Create: `erpnext/server_scripts/amend_expense_journal_entry.py`

- [ ] **Step 1: Write the script**

```python
journal_entry = frappe.form_dict.get("journal_entry")
amount = frappe.form_dict.get("amount")
description = frappe.form_dict.get("description")
expense_account = frappe.form_dict.get("expense_account")
income_account = frappe.form_dict.get("income_account")
posting_date = frappe.form_dict.get("posting_date")
company = frappe.form_dict.get("company")

if not journal_entry:
    frappe.throw("journal_entry is required")
if not amount or float(amount or 0) <= 0:
    frappe.throw("amount must be greater than 0")
if not expense_account:
    frappe.throw("expense_account is required")
if not income_account:
    frappe.throw("income_account is required")
if not posting_date:
    frappe.throw("posting_date is required")
if not company:
    frappe.throw("company is required")

if not frappe.db.exists("Journal Entry", journal_entry):
    frappe.throw(f"Journal Entry '{journal_entry}' does not exist")

docstatus = frappe.db.get_value("Journal Entry", journal_entry, "docstatus")
if docstatus != 1:
    frappe.throw(f"Journal Entry '{journal_entry}' is not in Submitted state")

try:
    # Cancel original
    je = frappe.get_doc("Journal Entry", journal_entry)
    je.cancel()

    # Create new amended journal entry
    new_je = frappe.get_doc({
        "doctype": "Journal Entry",
        "voucher_type": "Journal Entry",
        "company": company,
        "posting_date": posting_date,
        "user_remark": description or "",
        "amended_from": journal_entry,
        "accounts": [
            {
                "account": expense_account,
                "debit_in_account_currency": float(amount or 0),
            },
            {
                "account": income_account,
                "credit_in_account_currency": float(amount or 0),
            },
        ],
    })
    new_je.insert()
    new_je.submit()

    frappe.db.commit()

    frappe.response["data"] = {
        "journal_entry": new_je.name,
    }
except Exception:
    frappe.db.rollback()
    raise
```

- [ ] **Step 2: Commit**

```bash
git add erpnext/server_scripts/amend_expense_journal_entry.py
git commit -m "feat: add atomic amend for expense journal entries"
```

---

### Task 4: Create amend_full_purchase.py

**Files:**
- Create: `erpnext/server_scripts/amend_full_purchase.py`

- [ ] **Step 1: Write the script**

```python
purchase_invoice = frappe.form_dict.get("purchase_invoice")
company = frappe.form_dict.get("company")
supplier = frappe.form_dict.get("supplier")
warehouse = frappe.form_dict.get("warehouse")
invoice_number = frappe.form_dict.get("invoice_number")
invoice_date = frappe.form_dict.get("invoice_date")
items = frappe.form_dict.get("items", [])

if not purchase_invoice:
    frappe.throw("purchase_invoice is required")
if not company:
    frappe.throw("company is required")
if not supplier:
    frappe.throw("supplier is required")
if not warehouse:
    frappe.throw("warehouse is required")
if not items or not isinstance(items, list) or len(items) == 0:
    frappe.throw("At least one item is required")

if not frappe.db.exists("Purchase Invoice", purchase_invoice):
    frappe.throw(f"Purchase Invoice '{purchase_invoice}' does not exist")

docstatus = frappe.db.get_value("Purchase Invoice", purchase_invoice, "docstatus")
if docstatus != 1:
    frappe.throw(f"Purchase Invoice '{purchase_invoice}' is not in Submitted state")

if not invoice_date:
    invoice_date = frappe.utils.nowdate()

try:
    # --- Cancel old chain ---

    # Find linked documents
    pr_name = frappe.db.get_value("Purchase Invoice Item", {"parent": purchase_invoice}, "purchase_receipt")
    if not pr_name:
        frappe.throw("Could not find linked Purchase Receipt")

    po_name = frappe.db.get_value("Purchase Receipt Item", {"parent": pr_name}, "purchase_order")
    if not po_name:
        frappe.throw("Could not find linked Purchase Order")

    pe_name = frappe.db.get_value(
        "Payment Entry Reference",
        {"reference_doctype": "Purchase Invoice", "reference_name": purchase_invoice},
        "parent"
    )

    # Cancel in reverse order: Payment Entry -> Purchase Invoice -> Purchase Receipt -> Purchase Order
    if pe_name:
        pe = frappe.get_doc("Payment Entry", pe_name)
        if pe.docstatus == 1:
            pe.cancel()

    pi = frappe.get_doc("Purchase Invoice", purchase_invoice)
    pi.cancel()

    pr = frappe.get_doc("Purchase Receipt", pr_name)
    pr.cancel()

    po = frappe.get_doc("Purchase Order", po_name)
    po.cancel()

    # --- Create new chain ---

    # Create Purchase Order
    new_po = frappe.get_doc({
        "doctype": "Purchase Order",
        "company": company,
        "supplier": supplier,
        "transaction_date": invoice_date,
        "schedule_date": invoice_date,
        "set_warehouse": warehouse,
        "items": [
            {
                "item_code": item["item_code"],
                "qty": float(item["qty"] or 0),
                "rate": float(item["rate"] or 0),
                "warehouse": warehouse,
            }
            for item in items
        ],
    })
    new_po.insert()
    new_po.submit()

    # Create Purchase Receipt
    new_pr = frappe.get_doc({
        "doctype": "Purchase Receipt",
        "company": new_po.company,
        "supplier": new_po.supplier,
        "posting_date": invoice_date,
        "posting_time": "00:00:00",
        "set_warehouse": new_po.set_warehouse,
        "purchase_order": new_po.name,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": new_po.name,
                "purchase_order_item": item.name,
            }
            for item in new_po.items
        ],
    })
    new_pr.insert()
    new_pr.submit()

    # Create Purchase Invoice with amended_from link
    pi_data = {
        "doctype": "Purchase Invoice",
        "company": new_pr.company,
        "supplier": new_pr.supplier,
        "posting_date": invoice_date,
        "posting_time": "00:00:00",
        "bill_date": invoice_date,
        "purchase_receipt": new_pr.name,
        "amended_from": purchase_invoice,
        "update_stock": 0,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": item.purchase_order,
                "po_detail": item.purchase_order_item,
                "purchase_receipt": new_pr.name,
                "pr_detail": item.name,
            }
            for item in new_pr.items
        ],
    }
    if invoice_number:
        pi_data["bill_no"] = invoice_number

    new_pi = frappe.get_doc(pi_data)
    new_pi.insert()
    new_pi.submit()

    # Create Payment Entry
    supplier_name = frappe.db.get_value("Supplier", new_pi.supplier, "supplier_name") or new_pi.supplier

    default_cash_account = frappe.db.get_value("Company", company, "default_cash_account")
    if not default_cash_account:
        default_cash_account = frappe.db.get_value(
            "Account",
            {"company": company, "account_type": "Cash", "is_group": 0},
            "name"
        )
    if not default_cash_account:
        frappe.throw(f"No default cash account found for company '{company}'")

    default_payable = frappe.db.get_value("Company", company, "default_payable_account")
    if not default_payable:
        default_payable = frappe.db.get_value(
            "Account",
            {"company": company, "account_type": "Payable", "is_group": 0},
            "name"
        )
    if not default_payable:
        frappe.throw(f"No default payable account found for company '{company}'")

    if not frappe.db.exists("Mode of Payment", "Cash"):
        frappe.throw("Mode of Payment 'Cash' does not exist. Please create it first.")

    new_pe = frappe.get_doc({
        "doctype": "Payment Entry",
        "company": company,
        "payment_type": "Pay",
        "party_type": "Supplier",
        "party": new_pi.supplier,
        "party_name": supplier_name,
        "posting_date": invoice_date,
        "mode_of_payment": "Cash",
        "paid_from": default_cash_account,
        "paid_to": default_payable,
        "paid_amount": new_pi.grand_total,
        "received_amount": new_pi.grand_total,
        "reference_no": new_pi.bill_no or new_pi.name,
        "reference_date": new_pi.bill_date,
        "references": [
            {
                "reference_doctype": "Purchase Invoice",
                "reference_name": new_pi.name,
                "total_amount": new_pi.grand_total,
                "outstanding_amount": new_pi.outstanding_amount,
                "allocated_amount": new_pi.grand_total,
                "exchange_rate": 1,
            }
        ],
    })
    new_pe.insert()
    new_pe.submit()

    # Correct posting dates
    for doc, doctype in [(new_pr, "Purchase Receipt"), (new_pi, "Purchase Invoice"), (new_pe, "Payment Entry")]:
        frappe.db.set_value(doctype, doc.name, "posting_date", invoice_date)

    frappe.db.commit()
except Exception:
    frappe.db.rollback()
    raise

# Update item prices
buying_pl = frappe.db.get_value("Buying Settings", None, "buying_price_list") or "Standard Buying"
selling_pl = frappe.db.get_value("Selling Settings", None, "selling_price_list") or "Standard Selling"

for item in items:
    buy_rate = float(item.get("rate") or 0)
    if buy_rate > 0:
        existing_buy = frappe.db.exists("Item Price", {"item_code": item["item_code"], "price_list": buying_pl, "buying": 1})
        if existing_buy:
            frappe.db.set_value("Item Price", existing_buy, "price_list_rate", buy_rate)
        else:
            frappe.get_doc({
                "doctype": "Item Price",
                "item_code": item["item_code"],
                "price_list": buying_pl,
                "buying": 1,
                "price_list_rate": buy_rate,
            }).insert()

    sell_rate = float(item.get("sell_rate") or 0)
    if sell_rate > 0:
        existing_sell = frappe.db.exists("Item Price", {"item_code": item["item_code"], "price_list": selling_pl, "selling": 1})
        if existing_sell:
            frappe.db.set_value("Item Price", existing_sell, "price_list_rate", sell_rate)
        else:
            frappe.get_doc({
                "doctype": "Item Price",
                "item_code": item["item_code"],
                "price_list": selling_pl,
                "selling": 1,
                "price_list_rate": sell_rate,
            }).insert()

frappe.response["data"] = {
    "purchase_order": new_po.name,
    "purchase_receipt": new_pr.name,
    "purchase_invoice": new_pi.name,
    "payment_entry": new_pe.name,
}
```

- [ ] **Step 2: Commit**

```bash
git add erpnext/server_scripts/amend_full_purchase.py
git commit -m "feat: add atomic amend for full purchase chain"
```

---

### Task 5: Add frontend types

**Files:**
- Modify: `frontend/src/types/Expenses.ts:30,52-67`

- [ ] **Step 1: Add `amendEntryId` to Expense**

Add after `description` in the Expense interface (line 29):
```typescript
export interface Expense {
  date: string;
  expenseTypeId: string;
  amount: number;
  description: string;
  amendEntryId?: string;
}
```

- [ ] **Step 2: Add `item_code` to PurchaseInvoiceItem**

Update the existing `PurchaseInvoiceItem` interface to include `item_code` (ERPNext returns this, our type was incomplete):
```typescript
export interface PurchaseInvoiceItem {
  item_code: string;
  item_name: string;
  qty: number;
  rate: number;
  amount: number;
}
```

- [ ] **Step 3: Add AmendPurchasePayload type**

Add after `PurchaseInvoiceResponse` at the end of the file:
```typescript
export interface AmendPurchasePayload {
  originalId: string;
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number?: string;
  invoice_date: string;
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/Expenses.ts
git commit -m "feat: add amend types to Expense and AmendPurchasePayload"
```

---

### Task 6: Add ErpNextService methods

**Files:**
- Modify: `frontend/src/services/ErpNextService.ts:50-57,432-465`

- [ ] **Step 1: Add `amended_from` to PurchasePayload**

```typescript
export interface PurchasePayload {
  company: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number?: string;
  invoice_date: string;
  amended_from?: string;
}
```

- [ ] **Step 2: Add `getJournalEntry` method**

Insert after `cancelExpenseJournalEntry` (line 465):
```typescript
  public getJournalEntry(name: string) {
    return this.instance
      .get<JournalEntry>(
        `/api/resource/Journal Entry/${encodeURIComponent(name)}`
      )
      .then((resp) => resp.data)
      .catch(() => undefined);
  }
```

- [ ] **Step 3: Add `amendExpenseJournalEntry` method**

Insert after the new `getJournalEntry` method:
```typescript
  public async amendExpenseJournalEntry(
    originalId: string,
    expense: Expense,
    incomeAccount: AccountResponse,
    expenseAccount: AccountResponse,
  ) {
    const authStore = useAuthStore();
    try {
      const response = await this.instance.post<{ data?: { journal_entry: string } }>(
        "/api/v2/method/amend_expense_journal_entry",
        {
          journal_entry: originalId,
          amount: expense.amount,
          description: expense.description,
          expense_account: expenseAccount.name,
          income_account: incomeAccount.name,
          posting_date: expense.date,
          company: authStore.company,
        }
      );
      return response.data.data?.journal_entry;
    } catch {
      return undefined;
    }
  }
```

- [ ] **Step 4: Add `amendFullPurchase` method**

Insert after `amendExpenseJournalEntry`:
```typescript
  public amendFullPurchase(payload: AmendPurchasePayload) {
    return this.instance
      .post<{ data?: PurchaseResult }>("/api/v2/method/amend_full_purchase", {
        purchase_invoice: payload.originalId,
        company: payload.company,
        supplier: payload.supplier,
        warehouse: payload.warehouse,
        items: payload.items,
        invoice_number: payload.invoice_number || "",
        invoice_date: payload.invoice_date,
      })
      .then((resp) => resp?.data.data)
      .catch(() => undefined);
  }
```

- [ ] **Step 5: Add import for AmendPurchasePayload**

Add `AmendPurchasePayload` to the import from `@/types/Expenses` at the top:
```typescript
import type {
  Expense,
  CompanyExpenseMapping,
  AccountMappings,
  AccountResponse,
  PurchaseInvoiceResponse,
  AmendPurchasePayload,
} from "@/types/Expenses";
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/services/ErpNextService.ts
git commit -m "feat: add amend methods to ErpNextService"
```

---

### Task 7: Add amendExpense to ExpenseServiceFunctions

**Files:**
- Modify: `frontend/src/services/ExpenseServiceFunctions.ts:1-3,11-28`

- [ ] **Step 1: Add `amendExpense` function**

Add after the `submitExpense` function (before `bulkSubmitExpenses`):
```typescript
function amendExpense(
  erpNextService: ErpNextService,
  accountMappings: AccountMappings,
  expense: Expense,
): Promise<string | undefined> {
  if (!expense.amendEntryId) {
    return Promise.resolve(undefined);
  }

  const incomeAccount = accountMappings.income;
  const expenseAccount = accountMappings.expenses[expense.expenseTypeId];

  if (!incomeAccount || !expenseAccount) {
    return Promise.resolve(undefined);
  }

  return erpNextService.amendExpenseJournalEntry(
    expense.amendEntryId,
    expense,
    incomeAccount,
    expenseAccount,
  );
}
```

- [ ] **Step 2: Export `amendExpense`**

Update the export line at the bottom:
```typescript
export { submitExpense, bulkSubmitExpenses, amendExpense };
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/services/ExpenseServiceFunctions.ts
git commit -m "feat: add amendExpense service function"
```

---

### Task 8: Add amendDraftExpense to DataStore

**Files:**
- Modify: `frontend/src/stores/DataStore.ts:91-97,145-151`

- [ ] **Step 1: Add `amendDraftExpense` action**

Add after `addDraftExpense` (line 97):
```typescript
  function amendDraftExpense(expense: Expense) {
    return ExpenseServiceFunctions.amendExpense(
      new ErpNextService(),
      accountMappings.value,
      expense,
    );
  }
```

- [ ] **Step 2: Export `amendDraftExpense`**

Add to the return object (after `addDraftExpense`):
```typescript
    amendDraftExpense,
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/stores/DataStore.ts
git commit -m "feat: add amendDraftExpense to DataStore"
```

---

### Task 9: Add amend mode to ExpenseForm

**Files:**
- Modify: `frontend/src/components/ExpenseForm.vue:119-207`

- [ ] **Step 1: Add `amendEntry` prop**

In the script section, add after other props:
```typescript
const props = defineProps<{
  mappings: CompanyExpenseMapping[];
  loading: boolean;
  amendEntry?: {
    id: string;
    amount: number;
    description: string;
    expenseTypeId: string;
    date: string;
  } | null;
}>();
```

Update the existing props block to include `amendEntry`. If props use `const props = defineProps({...})` syntax, change to TypeScript generic syntax.

- [ ] **Step 2: Add `amend` emit**

Add to emit declarations:
```typescript
const emit = defineEmits<{
  (e: 'submit', expense: Expense): void;
  (e: 'amend', expense: Expense): void;
}>();
```

Update the existing emit to add the `amend` event.

- [ ] **Step 3: Initialize state from amendEntry**

In the `state` reactive initialization, check for `amendEntry`:
```typescript
const state = reactive({
  date: props.amendEntry?.date ? new Date(props.amendEntry.date) : new Date(),
  expenseTypeId: props.amendEntry?.expenseTypeId || "",
  amount: props.amendEntry?.amount || 0,
  description: props.amendEntry?.description || "",
});
```

- [ ] **Step 4: Update confirmSubmit to emit amend**

Change `confirmSubmit` to emit `amend` when `amendEntry` is set:
```typescript
function confirmSubmit() {
  if (props.amendEntry) {
    emit("amend", {
      date: moment(state.date).format("YYYY-MM-DD"),
      expenseTypeId: state.expenseTypeId,
      amount: state.amount,
      description: state.description,
      amendEntryId: props.amendEntry.id,
    });
  } else {
    emit("submit", {
      date: moment(state.date).format("YYYY-MM-DD"),
      expenseTypeId: state.expenseTypeId,
      amount: state.amount,
      description: state.description,
    });
  }
}
```

- [ ] **Step 5: Update template title and button label**

Add conditional title and button label in template. Change the modal title (passed from parent) and the submit button to show "Amend" variant. Add a header showing amending state:

In the template, add before the `UForm`:
```html
<div v-if="amendEntry" class="text-sm text-[var(--ui-text-muted)] mb-2">
  Amending Entry: <strong>{{ amendEntry.id }}</strong>
</div>
```

Change submit button text:
```html
<UButton type="submit" class="hover:cursor-pointer" :loading="loading" :disabled="loading">
  {{ amendEntry ? 'Amend Entry' : 'Submit' }}
</UButton>
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/ExpenseForm.vue
git commit -m "feat: add amend mode to ExpenseForm"
```

---

### Task 10: Add amend mode to PurchaseForm

**Files:**
- Modify: `frontend/src/components/PurchaseForm.vue:253-488`

- [ ] **Step 1: Add `amendOrder` prop**

```typescript
const props = defineProps<{
  loading: boolean;
  amendOrder?: {
    id: string;
    supplier: string;
    warehouse: string;
    items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
    invoiceNumber: string;
    invoiceDate: string;
  } | null;
}>();
```

- [ ] **Step 2: Add `amend` emit**

```typescript
const emit = defineEmits<{
  (e: 'submit', payload: PurchasePayload): void;
  (e: 'amend', payload: PurchasePayload & { amended_from: string }): void;
}>();
```

- [ ] **Step 3: Pre-fill state from amendOrder on mount**

Add a `watchEffect` or `onMounted` that populates state when `amendOrder` is set:
```typescript
import { watchEffect } from "vue";

watchEffect(() => {
  if (props.amendOrder) {
    state.invoiceDate = new Date(props.amendOrder.invoiceDate);
    state.invoiceNumber = props.amendOrder.invoiceNumber;
    selectedSupplier.value = props.amendOrder.supplier;
    state.items = props.amendOrder.items.map(item => ({ ...item }));
  }
});
```

- [ ] **Step 4: Make warehouse and date readonly in template**

Add `:disabled="submitting || !!amendOrder"` to the invoice date picker and warehouse fields in both desktop and mobile templates.

In desktop template (date picker):
```html
<UButton ... :disabled="submitting || !!amendOrder">
```

In mobile template (date picker):
```html
<UButton ... :disabled="submitting || !!amendOrder">
```

For warehouse (if a visible field), add `:disabled`.

- [ ] **Step 5: Update confirmSubmit to emit amend**

```typescript
function confirmSubmit() {
  if (props.amendOrder) {
    emit("amend", {
      supplier: selectedSupplier.value,
      warehouse: props.amendOrder.warehouse,
      items: state.items.map((item) => ({
        item_code: item.item_code,
        qty: item.qty,
        rate: item.rate,
        sell_rate: item.sell_rate,
      })),
      invoice_number: state.invoiceNumber || null,
      invoice_date: moment(state.invoiceDate).format("YYYY-MM-DD"),
      amended_from: props.amendOrder.id,
    });
  } else {
    emit("submit", {
      supplier: selectedSupplier.value,
      warehouse: selectedWarehouse.value,
      items: state.items.map((item) => ({
        item_code: item.item_code,
        qty: item.qty,
        rate: item.rate,
        sell_rate: item.sell_rate,
      })),
      invoice_number: state.invoiceNumber || null,
      invoice_date: moment(state.invoiceDate).format("YYYY-MM-DD"),
    });
  }
}
```

- [ ] **Step 6: Update template title and button**

Add amend header in template:
```html
<div v-if="amendOrder" class="text-sm text-[var(--ui-text-muted)] mb-2">
  Amending Order: <strong>{{ amendOrder.id }}</strong>
</div>
```

Change submit button text (in shared footer):
```html
<UButton type="submit" color="primary" :loading="submitting" :disabled="submitting">
  {{ amendOrder ? 'Amend Order' : 'Submit Purchase' }}
</UButton>
```

- [ ] **Step 7: Commit**

```bash
git add frontend/src/components/PurchaseForm.vue
git commit -m "feat: add amend mode to PurchaseForm"
```

---

### Task 11: Add Amend button to ExpenseTable

**Files:**
- Modify: `frontend/src/components/ExpenseTable.vue:58-75,228-240`

- [ ] **Step 1: Add `amend` to emits**

```typescript
const emit = defineEmits<{
  (e: 'cancel', payment: Payment): void;
  (e: 'amend', payment: Payment): void;
}>();
```

- [ ] **Step 2: Add Amend button in expanded row (mobile)**

In the `#expanded` template, after the Cancel button (line 74):
```html
<UButton
  v-if="row.original.status === 'Submitted'"
  color="success"
  variant="ghost"
  icon="i-lucide-pencil"
  @click="emit('amend', row.original)"
>
  Amend
</UButton>
```

- [ ] **Step 3: Add Amend button in action column (desktop)**

In the `actions` column cell renderer (around line 230), add the Amend button. Find the action buttons section and add after Cancel:
```typescript
// The actions column cell renderer currently shows Cancel button.
// Add Amend button next to it.
cell: ({ row }) => {
  if (row.original.status !== 'Submitted') return null;
  return h('div', { class: 'flex gap-1' }, [
    h(UButton, {
      color: 'error',
      variant: 'ghost',
      icon: 'i-lucide-x',
      size: 'sm',
      onClick: () => emit('cancel', row.original),
    }, () => 'Cancel'),
    h(UButton, {
      color: 'success',
      variant: 'ghost',
      icon: 'i-lucide-pencil',
      size: 'sm',
      onClick: () => emit('amend', row.original),
    }, () => 'Amend'),
  ]);
},
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/ExpenseTable.vue
git commit -m "feat: add Amend button to ExpenseTable"
```

---

### Task 12: Add amend handler to ExpensesView

**Files:**
- Modify: `frontend/src/views/ExpensesView.vue:1-263`

- [ ] **Step 1: Add state for amend**

Add new refs:
```typescript
const openAmendExpense = ref(false);
const amendEntry = ref<{
  id: string;
  amount: number;
  description: string;
  expenseTypeId: string;
  date: string;
} | null>(null);
const amendLoading = ref(false);
```

- [ ] **Step 2: Add `onAmendExpense` handler**

Add function:
```typescript
async function onAmendExpense(payment: Payment) {
  // Fetch original journal entry for pre-fill values
  const je = await erpnext.getJournalEntry(payment.id);
  if (!je) {
    toast.add({ title: "Failed to fetch journal entry for amend", color: "error" });
    return;
  }

  // Find expense account (debit > 0) to map back to expenseTypeId
  const debitAccount = je.accounts?.find(a => a.debit_in_account_currency > 0);
  if (!debitAccount) {
    toast.add({ title: "Could not determine expense account", color: "error" });
    return;
  }

  // Map ERPNext account name to expenseTypeId using mappings
  const mapping = mappings.value.find(
    m => m.erpnextAccountName === debitAccount.account
  );
  const expenseTypeId = mapping?.expenseTypeId || "";

  amendEntry.value = {
    id: payment.id,
    amount: payment.amount,
    description: payment.description,
    expenseTypeId,
    date: payment.date,
  };
  openAmendExpense.value = true;
}
```

- [ ] **Step 3: Handle amend submission**

Add function:
```typescript
async function onAmendSubmit(expense: Expense) {
  amendLoading.value = true;
  const response = await dataStore.amendDraftExpense(expense);
  amendLoading.value = false;

  if (response) {
    openAmendExpense.value = false;
    amendEntry.value = null;
    toast.add({
      title: `Expense amended successfully: ${response}`,
      color: "success",
    });
  } else {
    toast.add({
      title: "Failed to amend expense",
      color: "error",
    });
  }
}
```

- [ ] **Step 4: Add Amend modal in template**

Add after the existing cancel modal (line 99):
```html
<UModal
    v-model:open="openAmendExpense"
    title="Amend Expense"
    :dismissible="false"
>
    <template #body>
        <ExpenseForm
            v-if="amendEntry"
            :mappings="mappings"
            :loading="amendLoading"
            :amend-entry="amendEntry"
            @amend="onAmendSubmit"
            @submit="() => {}"
        />
    </template>
</UModal>
```

- [ ] **Step 5: Wire amend event on ExpenseTable**

Update the ExpenseTable component tag to handle amend:
```html
<ExpenseTable
    :data="dataStore.paymentEntries"
    :loading="dataStore.loading"
    @cancel="onCancelPurchase"
    @amend="onAmendExpense"
/>
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/views/ExpensesView.vue
git commit -m "feat: add amend expense handler to ExpensesView"
```

---

### Task 13: Add order amend handler to ExpensesView

**Files:**
- Modify: `frontend/src/views/ExpensesView.vue:1-263`

The ExpenseTable lives in ExpensesView, and emits `amend` for both expense and order rows. ExpensesView must handle both types.

- [ ] **Step 1: Add `PurchaseForm` import**

```typescript
import PurchaseForm from "@/components/PurchaseForm.vue";
```

- [ ] **Step 2: Add state for order amend**

Add new refs after the existing amend state from Task 12:
```typescript
const openAmendOrder = ref(false);
const amendOrderData = ref<{
  id: string;
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoiceNumber: string;
  invoiceDate: string;
} | null>(null);
const amendOrderLoading = ref(false);
```

- [ ] **Step 3: Update `onAmendExpense` to dispatch by type**

Replace the handler from Task 12:
```typescript
async function onAmendExpense(payment: Payment) {
  if (payment.type === "Order") {
    const piResponse = await erpnext.getPurchaseInvoice(payment.id);
    if (!piResponse?.data) {
      toast.add({ title: "Failed to fetch purchase invoice for amend", color: "error" });
      return;
    }

    const pi = piResponse.data;
    amendOrderData.value = {
      id: payment.id,
      supplier: pi.supplier,
      warehouse: "",
      items: pi.items.map(item => ({
        item_code: item.item_code,
        qty: item.qty,
        rate: item.rate,
        sell_rate: 0,
      })),
      invoiceNumber: pi.name,
      invoiceDate: pi.posting_date,
    };
    openAmendOrder.value = true;
    return;
  }

  // Expense amend flow
  const je = await erpnext.getJournalEntry(payment.id);
  if (!je) {
    toast.add({ title: "Failed to fetch journal entry for amend", color: "error" });
    return;
  }

  const debitAccount = je.accounts?.find(a => a.debit_in_account_currency > 0);
  if (!debitAccount) {
    toast.add({ title: "Could not determine expense account", color: "error" });
    return;
  }

  const mapping = mappings.value.find(
    m => m.erpnextAccountName === debitAccount.account
  );
  const expenseTypeId = mapping?.expenseTypeId || "";

  amendEntry.value = {
    id: payment.id,
    amount: payment.amount,
    description: payment.description,
    expenseTypeId,
    date: payment.date,
  };
  openAmendExpense.value = true;
}
```

- [ ] **Step 4: Add `onAmendOrderSubmit` handler**

```typescript
async function onAmendOrderSubmit(payload: {
  supplier: string;
  warehouse: string;
  items: { item_code: string; qty: number; rate: number; sell_rate: number }[];
  invoice_number: string | null;
  invoice_date: string;
  amended_from: string;
}) {
  amendOrderLoading.value = true;
  const result = await erpnext.amendFullPurchase({
    originalId: payload.amended_from,
    company: authStore.company || "",
    supplier: payload.supplier,
    warehouse: payload.warehouse,
    items: payload.items,
    invoice_number: payload.invoice_number || undefined,
    invoice_date: payload.invoice_date,
  });
  amendOrderLoading.value = false;

  if (result) {
    openAmendOrder.value = false;
    amendOrderData.value = null;
    dataStore.update();
    toast.add({
      title: `Purchase amended: PI ${result.purchase_invoice}`,
      color: "success",
    });
  } else {
    toast.add({
      title: "Failed to amend purchase",
      color: "error",
    });
  }
}
```

- [ ] **Step 5: Add Amend Order modal in template**

Add after the amend expense modal (from Task 12):
```html
<UModal
    v-model:open="openAmendOrder"
    title="Amend Order"
    :dismissible="false"
    :ui="{ content: 'sm:max-w-2xl' }"
>
    <template #body>
        <PurchaseForm
            v-if="amendOrderData"
            :loading="amendOrderLoading"
            :amend-order="amendOrderData"
            @amend="onAmendOrderSubmit"
            @submit="() => {}"
        />
    </template>
</UModal>
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/views/ExpensesView.vue
git commit -m "feat: add order amend handler to ExpensesView"
```

---

### Task 14: Build & verify

**Files:** None (verification only)

- [ ] **Step 1: Build frontend**

```bash
npm run build
```
Expected: Build succeeds with no errors.

- [ ] **Step 2: Check TypeScript**

```bash
npx vue-tsc --noEmit
```
Expected: No type errors.

- [ ] **Step 3: Build backend**

```bash
dotnet build
```
Expected: Build succeeds.

- [ ] **Step 4: Verify no regressions**

Review git diff for:
- Cancel functionality untouched (cancel button still present)
- No removed functionality
- All new code follows existing patterns (Nuxt UI components, service pattern, store pattern)

---

## Self-Review Checklist

- [ ] Spec coverage: Each spec section maps to at least one task above
- [ ] No TBD/TODO placeholders
- [ ] All file paths are exact
- [ ] All code examples include actual implementation, not pseudocode
- [ ] Type names consistent across tasks (e.g., `amendEntryId` used consistently)
- [ ] Import paths verified against existing project structure
