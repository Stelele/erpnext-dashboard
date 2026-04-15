# 1. Extract parameters passed from the frontend
company = frappe.form_dict.get('company')
start_date = frappe.form_dict.get('start_date')
end_date = frappe.form_dict.get('end_date')

# 2. Validate inputs
if not company or not start_date or not end_date:
    frappe.throw("Missing required parameters: company, start_date, and end_date")

# 3. Write the optimized SQL query joining the parent and child tables
query = """
    SELECT 
        je.name, 
        je.posting_date, 
        je.docstatus, 
        je.user_remark, 
        jea.account as debit_account,
        jea.debit as total_debit
    FROM 
        `tabJournal Entry` je
    INNER JOIN 
        `tabJournal Entry Account` jea ON je.name = jea.parent
    WHERE 
        je.company = %(company)s 
        AND je.posting_date BETWEEN %(start_date)s AND %(end_date)s
        AND jea.debit > 0
        AND je.docstatus < 2 -- Excludes cancelled documents (docstatus 2)
"""

# 4. Execute the query and map the results to a dictionary
entries = frappe.db.sql(query, {
    'company': company,
    'start_date': start_date,
    'end_date': end_date
}, as_dict=True)

# 5. Attach the results to the standard Frappe response message
frappe.response['data'] = entries