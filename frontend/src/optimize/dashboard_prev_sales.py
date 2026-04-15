months = frappe.form_dict.get("months", 6)
company = frappe.form_dict.get("company")

query = """
    SELECT 
        DATE_FORMAT(posting_date, '%%Y-%%m') AS grouping_name,
        SUM(total) AS total,
        COUNT(name) AS count
    FROM `tabPOS Invoice`
    WHERE status IN ('Paid', 'Consolidated')
    AND posting_date >= DATE_SUB(CURDATE(), INTERVAL %s MONTH)
    AND company = %s
    GROUP BY DATE_FORMAT(posting_date, '%%Y-%%m')
    ORDER BY grouping_name
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (months, company), 
    as_dict=True
)
