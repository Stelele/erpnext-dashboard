# Refunds View — Move Refund from POS Terminal to Dashboard

**Date:** 2026-11-13
**Status:** Draft (pending builder check)
**Requester:** Business owner (via Gift)

## Intent

The business owner wants to process POS invoice refunds from the Njeremoto
Dashboard instead of the POS terminal. Refund functionality is **removed from
the POS terminal entirely** and rebuilt in the dashboard as a new **Refunds**
view that hosts both the refund flow and a history of refunds over a period.

```
BEFORE                                          AFTER
┌──────────────┐                                ┌──────────────┐
│ POS terminal │── refund UI + API              │ POS terminal │── (no refund)
│ (pos_expenses│                                └──────────────┘
│  app)        │                                ┌──────────────┐   /api/method/
└──────────────┘                                │  Dashboard   │── awesome_dashboard
                                                │  (Vue 3)     │      .api.refunds.*
                                                └──────┬───────┘
                                                       │ browser → Frappe site
                                                       ▼ (direct, same as sales/stock)
                                                ┌──────────────┐
                                                │ awesome_     │
                                                │ dashboard    │
                                                │ (Frappe app) │
                                                └──────────────┘
```

## Repos touched

| Repo | Location | Change |
|---|---|---|
| `awesome_dashboard` (Frappe v16 app) | `~/Documents/code-projects/frappe/v16/apps/awesome_dashboard` (GitHub: Stelele/awesome_dashboard_scripts) | Add `api/refunds.py` with 4 whitelisted methods |
| `erpnext-dashboard` (Vue 3 frontend) | this repo | New `RefundsView.vue`, route, nav entry, service methods |
| `pos_expenses` (Frappe v16 app) | `~/Documents/code-projects/erpnext-point-of-sale-expenses` | Delete refund API methods + refund UI from POS page |

## Goals

- Owner can search submitted POS invoices, pick items + quantities, and submit
  a (partial or full) refund from the dashboard.
- Owner can see all refunds that occurred over a selected period.
- POS terminal page has no refund button, no refund modal, no refund API.

## Non-goals

- No reprint or expense changes — those stay in `pos_expenses` untouched.
- No new permission system in the dashboard (see Open decisions).
- No changes to the .NET backend — refunds flow browser → Frappe directly,
  same as every other ERPNext-backed feature in `ErpNextService.ts`.

## Anti-goals (failure even if it technically works)

- Double-refunding: the invoice detail must show returnable qty
  (original minus already-returned) and the server must re-validate on submit.
- Refunding a return invoice.

## Frappe side — `awesome_dashboard/awesome_dashboard/api/refunds.py`

New module. Move the three refund methods from `pos_expenses/api.py`
verbatim in logic, re-pointed imports (`erpnext.controllers.
sales_and_purchase_return` — identical on these sites):

| Endpoint | Signature | Source |
|---|---|---|
| `get_pos_invoices_for_refund` | `(company, from_date, to_date, status, search_term, limit=200)` → returnable submitted POS Invoices | moved, date widened to a range |
| `get_invoice_detail_for_refund` | `(invoice_name)` → items with `returnable_qty`, taxes, payments | moved as-is |
| `process_pos_refund` | `(invoice_name, return_items)` → `{"return_invoice": name}` | moved as-is |
| `get_refunds_for_period` **(new)** | `(company, from_date, to_date, limit=200)` → submitted POS Invoices with `is_return=1`, filtered on the **return's** `posting_date`, ordered newest first, with customer, grand_total, currency, posting date/time, and `return_against` (nullable) | new |

My calls, settled by the builder check:

- **`company` on every endpoint** — existing `awesome_dashboard` endpoints all
  take `company`; POS Invoice queries are scoped by it. Matches convention,
  removes any multi-company ambiguity.
- **Wire contract** — `return_items` stays a JSON *string* (the moved code's
  `json.loads` behaviour); the Vue service sends `JSON.stringify(...)`, exactly
  as the old POS JS did.
- **Double-submit** — client disables the submit button while a refund is in
  flight; the server's returnable-qty re-validation remains the backstop.
- **History contract** — return's posting date, `docstatus: 1` only,
  newest first. `limit=200` is ample for a period; no pagination this round.

All decorated `@frappe.whitelist(allow_guest=False)`, matching the existing
`awesome_dashboard` API convention. Permission enforcement comes from the
`Awesome Dashboard User` role granted in `installer.py` (see Permissions).

## Vue side — `erpnext-dashboard/frontend`

```
/refunds (new route, authGuard)
└─ RefundsView.vue
   ├─ Period selector (reuse SalesView period pattern)
   ├─ Refund history table  ← get_refunds_for_period
   │    invoice · return_against · customer · total · date/time
   └─ "New Refund" flow (modal or inline panel)
        ├─ invoice search ← get_pos_invoices_for_refund
        │   filters: date, time range, status, search term
        ├─ invoice detail + per-item qty picker (max = returnable_qty)
        │   ← get_invoice_detail_for_refund
        └─ submit → process_pos_refund → refresh history
```

- Service methods added to `ErpNextService.ts`, same axios/baseURL pattern as
  existing methods (auth via the per-company site URL already in AuthStore).
- Route + lazy import in `routes/index.ts`, nav entry alongside Sales/Stock.

## `pos_expenses` deletions

| File | Remove |
|---|---|
| `pos_expenses/api.py` | `get_pos_invoices_for_refund`, `get_invoice_detail_for_refund`, `process_pos_refund`; drop now-unused `make_return_doc`/`is_invoice_returnable`/`get_invoice_item_returned_qty` imports; keep expense + reprint methods |
| `pos_expenses/public/js/pos_extension.js` | Refund toolbar button + `open_refund_invoices_modal` and all `_refund_*` / `_build_refund_*` / `_load_refund_*` helpers (~500 lines) |
| README / docs | Refund feature bullets and screenshots |

## Constraints

- Both Frappe apps target ERPNext **version-16**.
- Refund correctness rests on ERPNext's own `make_return_doc` +
  returnable-qty validation — do not hand-roll return logic.
- Vue: TypeScript strict, no non-null assertions (hivemind code-style).

## Permissions (settled with the owner)

The dashboard token already writes on the sites — stock reconciliations,
purchase orders, journal entries all go through it. No user/role changes
needed beyond one line of app code: add to `installer.py`'s
`DASHBOARD_PERMISSIONS` dict:

```python
"POS Invoice": {"read": 1, "write": 1, "create": 1, "submit": 1},
```

The `after_migrate` hook applies it idempotently to the `Awesome Dashboard
User` role on every site at deploy time. Same mechanism as Journal Entry.

## Rollout order (confirmed by owner)

1. PR against `awesome_dashboard`: `api/refunds.py` + `POS Invoice`
   permissions → merge → deploy to all sites. Additive, breaks nothing.
2. Ship the dashboard's Refunds view.
3. Delete the refund methods/UI from `pos_expenses` (can lag; the dashboard
   stops calling those paths in step 2).

## Testing

Make the whole change across the three repos, then verify once against the
local v16 bench (`~/Documents/code-projects/frappe/v16`): refund a small
partial invoice, confirm history lists it, confirm the POS page no longer
shows the refund button.
