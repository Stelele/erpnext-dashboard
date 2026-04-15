from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
time_grouping = frappe.form_dict.get("time_grouping")


# 2. The Query using Common Table Expressions (CTE)
query = f"""
    WITH RunningBalance AS (
        SELECT
            posting_date,
            /* The Magic Line:
               Sum all 'stock_value_difference' from the beginning of time 
               up to the current row. This recreates the balance at that exact moment.
            */
            SUM(stock_value_difference) OVER (
                PARTITION BY company 
                ORDER BY posting_date, creation
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            ) as current_asset_value
        FROM `tabStock Ledger Entry`
        WHERE
            company = %s
            AND is_cancelled = 0
            AND posting_date <= %s /* Calculate history up to the End Date */
    )
    
    SELECT
        DATE_FORMAT(posting_date, '{time_grouping}') AS grouping_name,
        
        /* Average the balances found in that period */
        AVG(current_asset_value) AS average_stock_value,
        
        /* Optional: Show what the balance was at the very end of the period (Max date) */
        (SELECT current_asset_value FROM RunningBalance rb2 
         WHERE DATE_FORMAT(rb2.posting_date, '{time_grouping}') = grouping_name 
         ORDER BY posting_date DESC LIMIT 1) as closing_balance

    FROM RunningBalance
    WHERE 
        posting_date >= %s /* Only show results for the requested range */
    GROUP BY 
        DATE_FORMAT(posting_date, '{time_grouping}')
    ORDER BY 
        grouping_name
"""

frappe.response['data'] = frappe.db.sql(query, (company, to_date, from_date), as_dict=True)