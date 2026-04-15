from_date = frappe.form_dict.get("from_date")
to_date = frappe.form_dict.get("to_date")
company = frappe.form_dict.get("company")
warehouse = frappe.form_dict.get("warehouse", "Stores - NEs")

query = """
    WITH StockData AS (
        SELECT 
            DATE(sle.posting_date) AS stock_date,
            SUM(sle.stock_value_difference) AS total_stock_value,
            ROW_NUMBER() OVER (
                PARTITION BY DATE(sle.posting_date)
                ORDER BY sle.creation DESC
            ) AS rn
        FROM `tabStock Ledger Entry` sle
        WHERE sle.warehouse = %s
        AND sle.company = %s
        AND sle.is_cancelled = 0
        AND sle.posting_date BETWEEN %s AND %s
        GROUP BY DATE(sle.posting_date), sle.creation
    ),
    SalesData AS (
        SELECT 
            DATE(posting_date) AS sale_date,
            SUM(total) AS total_sales
        FROM `tabPOS Invoice`
        WHERE status IN ('Paid', 'Consolidated')
        AND company = %s
        AND posting_date BETWEEN %s AND %s
        GROUP BY DATE(posting_date)
    )
    SELECT 
        COALESCE(s.stock_date, sl.sale_date) AS grouping_name,
        ROUND(COALESCE(s.total_stock_value, 0), 2) AS average_stock_value,
        ROUND(COALESCE(sl.total_sales, 0), 2) AS total
    FROM StockData s
    FULL OUTER JOIN SalesData sl ON s.stock_date = sl.sale_date
    WHERE s.rn = 1 OR s.rn IS NULL
    ORDER BY grouping_name
"""

frappe.response['data'] = frappe.db.sql(
    query, 
    (warehouse, company, from_date, to_date, company, from_date, to_date), 
    as_dict=True
)
