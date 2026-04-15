warehouse = frappe.form_dict.get('warehouse')
company = frappe.form_dict.get('company')

if not warehouse or not company:
    frappe.throw("Both 'warehouse' and 'company' are required")


query = """
    SELECT 
        bin.item_code,
        item.item_name,
        (bin.projected_qty - COALESCE(pending.pending_qty, 0)) as real_qty,
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
            price_list_rate
        FROM `tabItem Price`
        WHERE
            price_list = 'Standard Buying'
    ) bp
        ON bin.item_code = bp.item_code
    LEFT JOIN(
       SELECT
            item_code,
            price_list_rate
        FROM `tabItem Price`
        WHERE
            price_list = 'Standard Selling' 
    ) sp
        ON bin.item_code = sp.item_code
    LEFT JOIN (
        SELECT 
            item.item_code, 
            SUM(item.qty) as pending_qty
        FROM `tabPOS Invoice` pos
        JOIN `tabPOS Invoice Item` item ON item.parent = pos.name
        WHERE 
            pos.status = 'Paid' 
            AND pos.update_stock = 1
            AND pos.company = %s 
            AND item.warehouse = %s
            -- Check if background job hasn't written the Ledger Entry yet
            AND NOT EXISTS (
                SELECT name FROM `tabStock Ledger Entry` sle 
                WHERE sle.voucher_no = pos.name 
                AND sle.voucher_type = 'POS Invoice'
            )
        GROUP BY item.item_code
    ) pending ON pending.item_code = bin.item_code
    WHERE 
        bin.warehouse = %s 
        AND bin.projected_qty > 0
"""

data = frappe.db.sql(query, (company, warehouse, warehouse), as_dict=True)

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