# ERPNext Server Scripts Standardization Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize ERPNext server scripts into a standardized base with njeremoto-specific overrides, eliminating duplication while preserving njeremoto functionality.

**Architecture:** Standardized scripts live in `erpnext/server_scripts/` and `erpnext/optimize/` as the canonical source. Njeremoto-specific versions of only 2 scripts live in `erpnext/njeremoto-overrides/server_scripts/`. All other scripts are shared.

**Tech Stack:** Python (Frappe framework), filesystem reorganization

---

## File Structure

### Existing files to move/rename

| Current Path | New Path | Notes |
|---|---|---|
| `erpnext/server_scripts/` (all 7) | `erpnext/server_scripts/` (stays, becomes canonical) | 2 will be modified, 5 stay as-is |
| `erpnext/optimize/` (all 6) | `erpnext/optimize/` (stays, becomes canonical) | All stay as-is |

### New files to create

| Path | Source | Notes |
|---|---|---|
| `erpnext/njeremoto-overrides/server_scripts/account_names.py` | Copy of current `erpnext/server_scripts/account_names.py` | Njeremoto-specific version |
| `erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py` | Copy of current `erpnext/server_scripts/get_stock_levels.py` | Njeremoto-specific version |

### Files to modify in place (become standardized)

| Path | Changes |
|---|---|
| `erpnext/server_scripts/account_names.py` | Remove hardcoded account lists (lines 3-20), remove unused `get_bounds()` (lines 22-31) |
| `erpnext/server_scripts/get_stock_levels.py` | Remove liquor filter and pack size logic (lines 75-92) |

---

### Task 1: Create njeremoto overrides directory and copy njeremoto-specific files

**Files:**
- Create: `erpnext/njeremoto-overrides/server_scripts/account_names.py`
- Create: `erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py`

- [ ] **Step 1: Create the overrides directory structure**

```bash
mkdir -p erpnext/njeremoto-overrides/server_scripts
```

- [ ] **Step 2: Copy account_names.py as njeremoto override**

Copy the current `erpnext/server_scripts/account_names.py` to `erpnext/njeremoto-overrides/server_scripts/account_names.py`:

```python
company = frappe.form_dict.get("company")

expense_accounts = [
    "Canteen",
    "Consumables",
    "Entertainment/Director's Expenses",
    "Spoiled Meat",
    "Utility Expenses",
    "Variance Deficit",
    "Miscellaneous Expenses"
]

income_accounts = [
    "Njeremoto Sales",
    "Variance Surplus",
    "Income"
]

income_accounts = [
    ]

def get_bounds(account_name):
    return frappe.db.get_value(
        "Account",
        {
            "company": company,
            "account_name": account_name
        },
        ["account_name", "name"],
        as_dict=True,
    )

frappe.response["data"] = {
    "expense": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Expense"
        },
        fields=["name", "account_name", "is_group"],
    ),
    "income": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Income"
        },
        fields=["name", "account_name", "is_group"],
    )
}
```

- [ ] **Step 3: Copy get_stock_levels.py as njeremoto override**

Copy the current `erpnext/server_scripts/get_stock_levels.py` to `erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py` (full 94 lines, unchanged from current).

- [ ] **Step 4: Verify the copies match originals**

```bash
diff erpnext/server_scripts/account_names.py erpnext/njeremoto-overrides/server_scripts/account_names.py
diff erpnext/server_scripts/get_stock_levels.py erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py
```

Expected: Both commands produce no output (files are identical).

- [ ] **Step 5: Commit**

```bash
git add erpnext/njeremoto-overrides/
git commit -m "feat: add njeremoto overrides for account_names and get_stock_levels"
```

---

### Task 2: Standardize account_names.py

**Files:**
- Modify: `erpnext/server_scripts/account_names.py`

- [ ] **Step 1: Replace account_names.py with cleaned standardized version**

Remove hardcoded account lists, dead code, and unused `get_bounds()`. Keep only the generic `root_type` queries:

```python
company = frappe.form_dict.get("company")

frappe.response["data"] = {
    "expense": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Expense"
        },
        fields=["name", "account_name", "is_group"],
    ),
    "income": frappe.get_all(
        "Account",
        filters={
            "company": company, 
            "root_type": "Income"
        },
        fields=["name", "account_name", "is_group"],
    )
}
```

- [ ] **Step 2: Verify the njeremoto override is still intact**

```bash
diff erpnext/server_scripts/account_names.py erpnext/njeremoto-overrides/server_scripts/account_names.py
```

Expected: Files differ (njeremoto version has hardcoded lists, standardized version does not).

- [ ] **Step 3: Commit**

```bash
git add erpnext/server_scripts/account_names.py
git commit -m "refactor: standardize account_names.py - remove hardcoded accounts and dead code"
```

---

### Task 3: Standardize get_stock_levels.py

**Files:**
- Modify: `erpnext/server_scripts/get_stock_levels.py`

- [ ] **Step 1: Replace get_stock_levels.py with standardized version**

Remove the liquor-specific filter and pack size logic (lines 75-92). Keep the query and price list lookups intact:

```python
warehouse = frappe.form_dict.get('warehouse')
company = frappe.form_dict.get('company')

if not warehouse or not company:
    frappe.throw("Both 'warehouse' and 'company' are required")

query = """
    SELECT 
        bin.item_code,
        item.item_name,
        (bin.actual_qty - COALESCE(pos_reserved.reserved_qty, 0)) as real_qty,
        item.item_group,
        sp.price_list_rate as selling_price,
        bp.price_list_rate as buying_price
    FROM 
        `tabBin` bin
    JOIN
        `tabItem` item
        ON bin.item_code = item.item_code
    LEFT JOIN (
        SELECT 
            item_code,
            SUM(reserved_qty) as reserved_qty
        FROM (
            SELECT 
                item_code,
                SUM(stock_qty) as reserved_qty
            FROM `tabPOS Invoice Item`
            WHERE warehouse = %s
            AND parent IN (
                SELECT name FROM `tabPOS Invoice` 
                WHERE docstatus = 1 
                AND (consolidated_invoice IS NULL OR consolidated_invoice = '')
                AND company = %s
            )
            GROUP BY item_code
            UNION ALL
            SELECT 
                item_code,
                SUM(qty) as reserved_qty
            FROM `tabPacked Item`
            WHERE warehouse = %s
            AND parent IN (
                SELECT name FROM `tabPOS Invoice` 
                WHERE docstatus = 1 
                AND (consolidated_invoice IS NULL OR consolidated_invoice = '')
                AND company = %s
            )
            GROUP BY item_code
        ) pos_items
        GROUP BY item_code
    ) pos_reserved ON bin.item_code = pos_reserved.item_code
    LEFT JOIN (
        SELECT
            item_code,
            price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Buying'
    ) bp ON bin.item_code = bp.item_code
    LEFT JOIN (
        SELECT
            item_code,
            price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Selling' 
    ) sp ON bin.item_code = sp.item_code
    WHERE 
        bin.warehouse = %s 
        AND bin.actual_qty > 0
    ORDER BY item_group ASC, item_name ASC
"""

frappe.response['data'] = frappe.db.sql(query, (warehouse, company, warehouse, company, warehouse), as_dict=True)
```

- [ ] **Step 2: Verify the njeremoto override is still intact**

```bash
diff erpnext/server_scripts/get_stock_levels.py erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py
```

Expected: Files differ (njeremoto version has liquor filter and pack size logic, standardized version does not).

- [ ] **Step 3: Commit**

```bash
git add erpnext/server_scripts/get_stock_levels.py
git commit -m "refactor: standardize get_stock_levels.py - remove liquor-specific filter and pack size logic"
```

---

### Task 4: Verify final structure

**Files:**
- All files under `erpnext/`

- [ ] **Step 1: Verify directory structure**

```bash
find erpnext/ -type f | sort
```

Expected output:
```
erpnext/njeremoto-overrides/server_scripts/account_names.py
erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py
erpnext/optimize/dashboard_bar_chart.py
erpnext/optimize/dashboard_complete.py
erpnext/optimize/dashboard_expense_breakdown.py
erpnext/optimize/dashboard_order_breakdown.py
erpnext/optimize/dashboard_payment_entries.py
erpnext/optimize/dashboard_sales_aggregated.py
erpnext/server_scripts/account_names.py
erpnext/server_scripts/get_average_stock_value.py
erpnext/server_scripts/get_daily_stock_value.py
erpnext/server_scripts/get_journal_entries.py
erpnext/server_scripts/get_stock_levels.py
erpnext/server_scripts/grouped_expenses_summary.py
erpnext/server_scripts/grouped_sales_summary.py
```

- [ ] **Step 2: Verify standardized account_names.py is clean**

```bash
wc -l erpnext/server_scripts/account_names.py
```

Expected: ~20 lines (no hardcoded lists, no dead code).

- [ ] **Step 3: Verify standardized get_stock_levels.py has no liquor references**

```bash
grep -n "Liquor\|Pint\|Quart\|Can\|Scud\|Super\|Rockers\|Dumpy\|pack_size" erpnext/server_scripts/get_stock_levels.py
```

Expected: No matches.

- [ ] **Step 4: Verify njeremoto overrides still have their specific logic**

```bash
grep -c "Njeremoto Sales" erpnext/njeremoto-overrides/server_scripts/account_names.py
grep -c "Liquor" erpnext/njeremoto-overrides/server_scripts/get_stock_levels.py
```

Expected: Both return count > 0.

- [ ] **Step 5: Final commit**

```bash
git add erpnext/
git commit -m "feat: finalize server scripts standardization with njeremoto overrides"
```
