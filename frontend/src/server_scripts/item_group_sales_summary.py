from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
time_grouping = frappe.form_dict.get("time_grouping")
query = f"""
        SELECT
            DATE_FORMAT(pi.posting_date, '{time_grouping}') AS grouping_name,
            i.item_group AS item_group,
            SUM(pii.amount) AS total,
            SUM(pii.qty) AS qty
        FROM `tabPOS Invoice` pi
        INNER JOIN `tabPOS Invoice Item` pii
            ON pii.parent = pi.name
        INNER JOIN `tabItem` i
            ON i.name = pii.item_code
        WHERE
            pi.status IN ('Paid', 'Consolidated')
            AND company = %s
            AND pi.posting_date BETWEEN %s AND %s
        GROUP BY
            DATE_FORMAT(pi.posting_date, '{time_grouping}'),
            i.item_group
        ORDER BY
            grouping_name ASC,
            item_group ASC
    """

frappe.response['data'] = frappe.db.sql(query, (company, from_date, to_date), as_dict=True)