from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")

query = """
    SELECT 
        je.name AS id,
        je.posting_date AS date,
        CASE je.docstatus
            WHEN 0 THEN 'Draft'
            WHEN 1 THEN 'Submitted'
            WHEN 2 THEN 'Cancelled'
        END AS status,
        'Expense' AS type,
        jea.account AS account,
        COALESCE(je.user_remark, '') AS description,
        jea.debit AS amount
    FROM `tabJournal Entry` je
    INNER JOIN `tabJournal Entry Account` jea ON je.name = jea.parent
    WHERE je.posting_date BETWEEN %s AND %s
    AND je.company = %s AND je.docstatus < 2 AND jea.debit > 0
    
    UNION ALL
    
    SELECT 
        name AS id,
        posting_date AS date,
        CASE docstatus
            WHEN 0 THEN 'Draft'
            WHEN 1 THEN 'Submitted'
            WHEN 2 THEN 'Cancelled'
        END AS status,
        'Order' AS type,
        '' AS account,
        COALESCE(supplier, '') AS description,
        grand_total AS amount
    FROM `tabPurchase Invoice`
    WHERE posting_date BETWEEN %s AND %s
    AND company = %s AND docstatus < 2
    
    ORDER BY date DESC, id DESC
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (from_date, to_date, company, from_date, to_date, company), 
    as_dict=True
)
