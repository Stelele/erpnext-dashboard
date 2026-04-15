from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
prev_from_date = frappe.form_dict.get("prev_from_date")
prev_to_date = frappe.form_dict.get("prev_to_date")
company = frappe.form_dict.get("company")
warehouse = frappe.form_dict.get("warehouse", "Stores - NEs")

results = []

# 1. Sales Summary (current + prev)
current_sales = frappe.db.sql("""
    SELECT COALESCE(SUM(total), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated') AND posting_date BETWEEN %s AND %s AND company = %s
""", (from_date, to_date, company), as_dict=True)[0]

prev_sales = frappe.db.sql("""
    SELECT COALESCE(SUM(total), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated') AND posting_date BETWEEN %s AND %s AND company = %s
""", (prev_from_date, prev_to_date, company), as_dict=True)[0]

results.append({
    "metric_type": "sales_summary",
    "cur_total": current_sales.total,
    "cur_count": current_sales.count,
    "prev_total": prev_sales.total,
    "prev_count": prev_sales.count
})

# 2. Purchase Summary (current + prev)
current_purchases = frappe.db.sql("""
    SELECT COALESCE(SUM(grand_total), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabPurchase Invoice`
    WHERE docstatus = 1 AND posting_date BETWEEN %s AND %s AND company = %s
""", (from_date, to_date, company), as_dict=True)[0]

prev_purchases = frappe.db.sql("""
    SELECT COALESCE(SUM(grand_total), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabPurchase Invoice`
    WHERE docstatus = 1 AND posting_date BETWEEN %s AND %s AND company = %s
""", (prev_from_date, prev_to_date, company), as_dict=True)[0]

results.append({
    "metric_type": "purchase_summary",
    "cur_total": current_purchases.total,
    "cur_count": current_purchases.count,
    "prev_total": prev_purchases.total,
    "prev_count": prev_purchases.count
})

# 3. Expense Summary (current + prev)
def get_bounds(account_name):
    result = frappe.db.get_value(
        "Account",
        {"company": company, "account_name": account_name, "is_group": 1},
        ["lft", "rgt"],
        as_dict=False,
    )
    if result:
        return {"lft": result[0], "rgt": result[1]}
    return None

indirect = get_bounds("Indirect Expenses")
direct = get_bounds("Direct Expenses")
stock = get_bounds("Stock Expenses")

if not indirect and not direct:
    frappe.throw(f"Could not find 'Indirect Expenses' or 'Direct Expenses' roots for company {company}")

expense_params = [company, from_date, to_date]
expense_conditions = []

if indirect:
    expense_conditions.append("(acc.lft BETWEEN %s AND %s)")
    expense_params.extend([indirect["lft"], indirect["rgt"]])

if direct:
    if stock:
        expense_conditions.append("(acc.lft BETWEEN %s AND %s AND NOT (acc.lft BETWEEN %s AND %s))")
        expense_params.extend([direct["lft"], direct["rgt"], stock["lft"], stock["rgt"]])
    else:
        expense_conditions.append("(acc.lft BETWEEN %s AND %s)")
        expense_params.extend([direct["lft"], direct["rgt"]])

expense_condition = " OR ".join(expense_conditions)

current_expenses = frappe.db.sql(f"""
    SELECT COALESCE(SUM(gle.debit) - SUM(gle.credit), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabGL Entry` gle
    INNER JOIN `tabAccount` acc ON acc.name = gle.account
    WHERE gle.company = %s AND gle.posting_date BETWEEN %s AND %s AND gle.is_cancelled = 0
    AND ({expense_condition})
""", tuple(expense_params), as_dict=True)[0]

prev_expense_params = [company, prev_from_date, prev_to_date] + expense_params[3:]
prev_expenses = frappe.db.sql(f"""
    SELECT COALESCE(SUM(gle.debit) - SUM(gle.credit), 0) as total, COALESCE(COUNT(*), 0) as count
    FROM `tabGL Entry` gle
    INNER JOIN `tabAccount` acc ON acc.name = gle.account
    WHERE gle.company = %s AND gle.posting_date BETWEEN %s AND %s AND gle.is_cancelled = 0
    AND ({expense_condition})
""", tuple(prev_expense_params), as_dict=True)[0]

results.append({
    "metric_type": "expense_summary",
    "cur_total": current_expenses.total,
    "cur_count": current_expenses.count,
    "prev_total": prev_expenses.total,
    "prev_count": prev_expenses.count
})

# Profit calculations
cur_gross_profit = current_sales.total - current_purchases.total
prev_gross_profit = prev_sales.total - prev_purchases.total
cur_net_profit = cur_gross_profit - current_expenses.total
prev_net_profit = prev_gross_profit - prev_expenses.total

results.append({
    "metric_type": "gross_profit_summary",
    "cur_total": cur_gross_profit,
    "prev_total": prev_gross_profit
})
results.append({
    "metric_type": "gross_margin_summary",
    "cur_total": current_sales.total and (cur_gross_profit / current_sales.total * 100) or 0,
    "prev_total": prev_sales.total and (prev_gross_profit / prev_sales.total * 100) or 0
})
results.append({
    "metric_type": "net_profit_summary",
    "cur_total": cur_net_profit,
    "prev_total": prev_net_profit
})
results.append({
    "metric_type": "net_margin_summary",
    "cur_total": current_sales.total and (cur_net_profit / current_sales.total * 100) or 0,
    "prev_total": prev_sales.total and (prev_net_profit / prev_sales.total * 100) or 0
})

# 4. Sales by Month
month_sales = frappe.db.sql("""
    SELECT DATE_FORMAT(posting_date, '%%Y-%%m') as grouping_name, SUM(total) as total, COUNT(*) as count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated') AND posting_date >= DATE_SUB(CURDATE(), INTERVAL 6 MONTH) AND company = %s
    GROUP BY DATE_FORMAT(posting_date, '%%Y-%%m')
    ORDER BY grouping_name
""", (company,), as_dict=True)

for row in month_sales:
    results.append({
        "metric_type": "sales_by_month",
        "grouping_name": row.grouping_name,
        "total": row.total,
        "count": row.count
    })

# 6. Sales by Category
category_sales = frappe.db.sql("""
    SELECT i.item_group, SUM(pii.amount) as total
    FROM `tabPOS Invoice` pi
    INNER JOIN `tabPOS Invoice Item` pii ON pii.parent = pi.name
    INNER JOIN `tabItem` i ON i.name = pii.item_code
    WHERE pi.status IN ('Paid', 'Consolidated') AND pi.posting_date BETWEEN %s AND %s AND pi.company = %s
    GROUP BY i.item_group
    ORDER BY total DESC
""", (from_date, to_date, company), as_dict=True)

for row in category_sales:
    results.append({
        "metric_type": "sales_by_category",
        "item_group": row.item_group,
        "total": row.total
    })

# 7. Stock by Group
stock_groups = frappe.db.sql("""
    SELECT item.item_group, ROUND(SUM(bin.projected_qty * COALESCE(bp.price_list_rate, 0)), 2) as total
    FROM `tabBin` bin
    JOIN `tabItem` item ON bin.item_code = item.item_code
    LEFT JOIN (
        SELECT item_code, price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Buying'
    ) bp ON bin.item_code = bp.item_code
    WHERE bin.warehouse = %s AND bin.projected_qty > 0
    GROUP BY item.item_group
    ORDER BY total DESC
""", (warehouse,), as_dict=True)

for row in stock_groups:
    results.append({
        "metric_type": "stock_by_group",
        "item_group": row.item_group,
        "total": row.total
    })

# 9. Stock Summary
stock_summary = frappe.db.sql("""
    SELECT 
        ROUND(SUM(bin.projected_qty * COALESCE(bp.price_list_rate, 0)), 2) as total_value,
        ROUND(SUM(bin.projected_qty * COALESCE(sp.price_list_rate, 0)), 2) as selling_value
    FROM `tabBin` bin
    LEFT JOIN (
        SELECT item_code, price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Buying'
    ) bp ON bin.item_code = bp.item_code
    LEFT JOIN (
        SELECT item_code, price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Selling'
    ) sp ON bin.item_code = sp.item_code
    WHERE bin.warehouse = %s
""", (warehouse,), as_dict=True)[0]

results.append({
    "metric_type": "stock_summary",
    "total_value": stock_summary.total_value or 0,
    "selling_value": stock_summary.selling_value or 0
})

frappe.response['data'] = results
