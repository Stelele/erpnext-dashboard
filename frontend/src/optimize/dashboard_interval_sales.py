from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
time_grouping = frappe.form_dict.get("time_grouping", "%Y-%m-%d")

query = f"""
    SELECT 
        DATE_FORMAT(posting_date, '{time_grouping}') AS grouping_name,
        SUM(total) AS total,
        COUNT(name) AS count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated')
    AND company = %s
    AND posting_date BETWEEN %s AND %s
    GROUP BY DATE_FORMAT(posting_date, '{time_grouping}')
    ORDER BY grouping_name
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (company, from_date, to_date), 
    as_dict=True
)
