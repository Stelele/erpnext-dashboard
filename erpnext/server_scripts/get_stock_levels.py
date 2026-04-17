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

data = frappe.db.sql(query, (warehouse, company, warehouse, company, warehouse), as_dict=True)

for entry in data:
    if entry.item_group != 'Liquor':
        continue
    
    if 'Pint' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 24, 2)} crates"
    elif 'Quart' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 12, 2)} crates"
    elif 'Can' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 6, 2)} cases"
    elif 'Scud' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 8, 2)} crates"
    elif 'Super' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 6, 2)} cases"
    elif 'Rockers' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 30, 2)} cases"
    elif 'Dumpy' in entry.item_name:
        entry['pack_size'] = f"{round(entry.real_qty / 24, 2)} cases"

frappe.response['data'] = data
