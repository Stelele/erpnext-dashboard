company = frappe.form_dict.get("company")
warehouse = frappe.form_dict.get("warehouse", "Stores - NEs")

query = """
    SELECT 
        item.item_group,
        ROUND(SUM(bin.projected_qty * COALESCE(bp.price_list_rate, 0)), 2) AS total
    FROM `tabBin` bin
    JOIN `tabItem` item ON bin.item_code = item.item_code
    LEFT JOIN (
        SELECT item_code, price_list_rate
        FROM `tabItem Price`
        WHERE price_list = 'Standard Buying'
    ) bp ON bin.item_code = bp.item_code
    WHERE bin.warehouse = %s
    AND bin.projected_qty > 0
    GROUP BY item.item_group
    ORDER BY total DESC
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (warehouse,), 
    as_dict=True
)
