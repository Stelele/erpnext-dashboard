journal_entry = frappe.form_dict.get("journal_entry")
amount = frappe.form_dict.get("amount")
description = frappe.form_dict.get("description")
expense_account = frappe.form_dict.get("expense_account")
income_account = frappe.form_dict.get("income_account")
posting_date = frappe.form_dict.get("posting_date")
company = frappe.form_dict.get("company")

if not journal_entry:
    frappe.throw("journal_entry is required")
if not amount or float(amount or 0) <= 0:
    frappe.throw("amount must be greater than 0")
if not expense_account:
    frappe.throw("expense_account is required")
if not income_account:
    frappe.throw("income_account is required")
if not posting_date:
    frappe.throw("posting_date is required")
if not company:
    frappe.throw("company is required")

if not frappe.db.exists("Company", company):
    frappe.throw(f"Company '{company}' does not exist")
if not frappe.db.exists("Account", expense_account):
    frappe.throw(f"Account '{expense_account}' does not exist")
if not frappe.db.exists("Account", income_account):
    frappe.throw(f"Account '{income_account}' does not exist")

if not frappe.db.exists("Journal Entry", journal_entry):
    frappe.throw(f"Journal Entry '{journal_entry}' does not exist")

docstatus = frappe.db.get_value("Journal Entry", journal_entry, "docstatus")
if docstatus != 1:
    frappe.throw(f"Journal Entry '{journal_entry}' is not in Submitted state")

try:
    # Cancel original
    je = frappe.get_doc("Journal Entry", journal_entry)
    je.cancel()

    # Create new amended journal entry
    new_je = frappe.get_doc({
        "doctype": "Journal Entry",
        "voucher_type": "Journal Entry",
        "company": company,
        "posting_date": posting_date,
        "user_remark": description or "",
        "amended_from": journal_entry,
        "amendment_date": posting_date,
        "accounts": [
            {
                "account": expense_account,
                "debit_in_account_currency": float(amount or 0),
            },
            {
                "account": income_account,
                "credit_in_account_currency": float(amount or 0),
            },
        ],
    })
    new_je.insert()
    new_je.submit()

    frappe.db.commit()

    frappe.response["data"] = {
        "journal_entry": new_je.name,
    }
except Exception:
    frappe.db.rollback()
    raise
