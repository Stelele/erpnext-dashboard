from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

query = """
    SELECT 
        supplier AS description,
        SUM(grand_total) AS total,
        COUNT(name) AS count
    FROM `tabPurchase Invoice`
    WHERE docstatus = 1
    AND company = %s
    AND posting_date BETWEEN %s AND %s
    GROUP BY supplier
    ORDER BY total DESC
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (company, from_date, to_date), 
    as_dict=True
)
