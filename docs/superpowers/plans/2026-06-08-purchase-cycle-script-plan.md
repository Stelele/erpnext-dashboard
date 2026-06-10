# Purchase Cycle Simplification Script Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a Frappe server script that simplifies the purchase cycle by creating Purchase Order → Purchase Receipt → Purchase Invoice → Payment Entry in a single API call.

**Architecture:** Single server script at `erpnext/server_scripts/create_full_purchase.py` with internal helper functions for each document creation step. Follows existing codebase patterns (frappe.form_dict for params, frappe.response for output, frappe.throw for errors).

**Tech Stack:** Frappe/ERPNext v15/v16, Python, Server Script pattern

---

### Task 1: Create the server script with input validation and helper functions

**Files:**
- Create: `erpnext/server_scripts/create_full_purchase.py`

- [ ] **Step 1: Write the complete server script**

Create `erpnext/server_scripts/create_full_purchase.py` with the following structure:

```python
import json
import frappe
from frappe.utils import nowdate, flt


def validate_inputs(company, supplier, warehouse, items, invoice_number, invoice_date):
    """Validate all required inputs exist and are valid."""
    if not company:
        frappe.throw("Company is required")
    if not frappe.db.exists("Company", company):
        frappe.throw(f"Company '{company}' does not exist")

    if not supplier:
        frappe.throw("Supplier is required")
    if not frappe.db.exists("Supplier", supplier):
        frappe.throw(f"Supplier '{supplier}' does not exist")

    if not warehouse:
        frappe.throw("Warehouse is required")
    if not frappe.db.exists("Warehouse", warehouse):
        frappe.throw(f"Warehouse '{warehouse}' does not exist")

    if not items or len(items) == 0:
        frappe.throw("At least one item is required")

    for item in items:
        if not item.get("item_code"):
            frappe.throw("item_code is required for each item")
        if not frappe.db.exists("Item", item["item_code"]):
            frappe.throw(f"Item '{item['item_code']}' does not exist")
        if not item.get("qty") or flt(item["qty"]) <= 0:
            frappe.throw(f"qty must be greater than 0 for item '{item['item_code']}'")
        if item.get("rate") is None or flt(item["rate"]) < 0:
            frappe.throw(f"rate must be 0 or greater for item '{item['item_code']}'")

    if not invoice_number:
        frappe.throw("Invoice number is required")
    if not invoice_date:
        frappe.throw("Invoice date is required")


def create_purchase_order(company, supplier, warehouse, items):
    """Create and submit a Purchase Order."""
    po = frappe.get_doc({
        "doctype": "Purchase Order",
        "company": company,
        "supplier": supplier,
        "schedule_date": nowdate(),
        "set_warehouse": warehouse,
        "items": [
            {
                "item_code": item["item_code"],
                "qty": flt(item["qty"]),
                "rate": flt(item["rate"]),
                "warehouse": warehouse,
            }
            for item in items
        ],
    })
    po.insert()
    po.submit()
    return po


def create_purchase_receipt(po):
    """Create and submit a Purchase Receipt from a Purchase Order."""
    pr = frappe.get_doc({
        "doctype": "Purchase Receipt",
        "company": po.company,
        "supplier": po.supplier,
        "posting_date": nowdate(),
        "set_warehouse": po.set_warehouse,
        "purchase_order": po.name,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_order": po.name,
                "purchase_order_item": item.name,
            }
            for item in po.items
        ],
    })
    pr.insert()
    pr.submit()
    return pr


def create_purchase_invoice(pr, invoice_number, invoice_date):
    """Create and submit a Purchase Invoice from a Purchase Receipt with user's invoice details."""
    pi = frappe.get_doc({
        "doctype": "Purchase Invoice",
        "company": pr.company,
        "supplier": pr.supplier,
        "posting_date": invoice_date,
        "bill_no": invoice_number,
        "bill_date": invoice_date,
        "purchase_receipt": pr.name,
        "update_stock": 0,
        "items": [
            {
                "item_code": item.item_code,
                "qty": item.qty,
                "rate": item.rate,
                "warehouse": item.warehouse,
                "purchase_receipt": pr.name,
                "pr_detail": item.name,
            }
            for item in pr.items
        ],
    })
    pi.insert()
    pi.submit()
    return pi


def create_payment_entry(pi):
    """Create and submit a cash Payment Entry against the Purchase Invoice."""
    company = pi.company

    # Get default cash account from company
    default_cash_account = frappe.db.get_value("Company", company, "default_cash_account")
    if not default_cash_account:
        # Fallback: find any cash account
        default_cash_account = frappe.db.get_value(
            "Account",
            {"company": company, "account_type": "Cash", "is_group": 0},
            "name"
        )

    if not default_cash_account:
        frappe.throw(f"No default cash account found for company '{company}'")

    # Get default payable account
    default_payable = frappe.db.get_value("Company", company, "default_payable_account")
    if not default_payable:
        default_payable = frappe.get_value(
            "Account",
            {"company": company, "account_type": "Payable", "is_group": 0},
            "name"
        )

    if not default_payable:
        frappe.throw(f"No default payable account found for company '{company}'")

    pe = frappe.get_doc({
        "doctype": "Payment Entry",
        "company": company,
        "payment_type": "Pay",
        "party_type": "Supplier",
        "party": pi.supplier,
        "party_name": pi.supplier_name or pi.supplier,
        "posting_date": nowdate(),
        "mode_of_payment": "Cash",
        "paid_from": default_payable,
        "paid_to": default_cash_account,
        "paid_amount": pi.grand_total,
        "received_amount": pi.grand_total,
        "reference_no": pi.bill_no,
        "reference_date": pi.bill_date,
        "references": [
            {
                "reference_doctype": "Purchase Invoice",
                "reference_name": pi.name,
                "total_amount": pi.grand_total,
                "outstanding_amount": pi.outstanding_amount,
                "allocated_amount": pi.grand_total,
            }
        ],
    })
    pe.insert()
    pe.submit()
    return pe


# --- Main entry point (Frappe Server Script pattern) ---

# Parse inputs
company = frappe.form_dict.get("company")
supplier = frappe.form_dict.get("supplier")
warehouse = frappe.form_dict.get("warehouse")
items_raw = frappe.form_dict.get("items")
invoice_number = frappe.form_dict.get("invoice_number")
invoice_date = frappe.form_dict.get("invoice_date")

# Parse items JSON if string
if isinstance(items_raw, str):
    items = json.loads(items_raw)
else:
    items = items_raw

# Validate
validate_inputs(company, supplier, warehouse, items, invoice_number, invoice_date)

# Execute purchase cycle
po = create_purchase_order(company, supplier, warehouse, items)
pr = create_purchase_receipt(po)
pi = create_purchase_invoice(pr, invoice_number, invoice_date)
pe = create_payment_entry(pi)

# Return result
frappe.response["data"] = {
    "purchase_order": po.name,
    "purchase_receipt": pr.name,
    "purchase_invoice": pi.name,
    "payment_entry": pe.name,
}
```

- [ ] **Step 2: Verify file follows existing patterns**

Check against existing scripts in `erpnext/server_scripts/`:
- Uses `frappe.form_dict.get()` for parameter extraction ✓
- Uses `frappe.response["data"]` for response ✓
- Uses `frappe.throw()` for validation errors ✓
- Uses `frappe.db.exists()` for validation ✓
- Uses `frappe.get_doc()` for document creation ✓
- No classes, procedural style ✓
- snake_case naming ✓

- [ ] **Step 3: Commit**

```bash
git add erpnext/server_scripts/create_full_purchase.py
git commit -m "feat: add purchase cycle simplification server script"
```

---

### Task 2: Add API endpoint documentation

**Files:**
- Modify: `README.md` (or create API docs if they exist)

- [ ] **Step 1: Document the API endpoint**

Add to project documentation:

```markdown
## Purchase Cycle API

### Create Full Purchase Cycle

Creates Purchase Order → Purchase Receipt → Purchase Invoice → Payment Entry in one call.

**Endpoint:** `/api/method/runservermethod?script_name=create_full_purchase`

**Method:** POST

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| company | string | Yes | Company name |
| supplier | string | Yes | Supplier name |
| warehouse | string | Yes | Receiving warehouse |
| items | JSON string | Yes | Array of {item_code, qty, rate} |
| invoice_number | string | Yes | Supplier's invoice number |
| invoice_date | string | Yes | Supplier's invoice date (YYYY-MM-DD) |

**Example Request:**
```json
{
  "company": "My Company",
  "supplier": "ABC Suppliers",
  "warehouse": "Stores - MC",
  "items": "[{\"item_code\": \"ITEM-001\", \"qty\": 10, \"rate\": 100}, {\"item_code\": \"ITEM-002\", \"qty\": 5, \"rate\": 200}]",
  "invoice_number": "SUP-INV-2024-001",
  "invoice_date": "2024-01-15"
}
```

**Example Response:**
```json
{
  "data": {
    "purchase_order": "PO-00001",
    "purchase_receipt": "PR-00001",
    "purchase_invoice": "PI-00001",
    "payment_entry": "PE-00001"
  }
}
```

**Error Response:**
```json
{
  "exc": "[\"frappe.exceptions.ValidationError: Supplier 'XYZ' does not exist\"]"
}
```
```

- [ ] **Step 2: Commit**

```bash
git add README.md
git commit -m "docs: add purchase cycle API documentation"
```

---

## Self-Review

**1. Spec coverage:**
- Single server script ✓
- Internal helper functions ✓
- Input validation ✓
- PO creation ✓
- PR creation ✓
- PI creation with invoice details ✓
- Cash payment entry ✓
- API contract (input/output) ✓
- Error handling with frappe.throw ✓
- Follows existing patterns ✓

**2. Placeholder scan:** No TBDs, TODOs, or vague references found.

**3. Type consistency:** All functions use consistent parameter names. Document names returned as strings.

**Plan is complete and ready for execution.**
