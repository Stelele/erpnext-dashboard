from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

query = """
    SELECT 
        parent.posting_date,
        child.item_name,
        child.item_group,
        SUM(child.qty) as qty,
        SUM(child.amount) as item_total_amount
    FROM `tabPOS Invoice` parent
    INNER JOIN `tabPOS Invoice Item` child ON child.parent = parent.name
    WHERE parent.status IN ('Paid', 'Consolidated')
    AND parent.company = %s
    AND parent.posting_date BETWEEN %s AND %s
    GROUP BY parent.posting_date, child.item_name, child.item_group
    ORDER BY parent.posting_date DESC, child.item_group
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (company, from_date, to_date), 
    as_dict=True
)
