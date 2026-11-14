# Refunds View Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move POS invoice refund out of the `pos_expenses` app (API + POS-page UI) into the `awesome_dashboard` Frappe app and a new dashboard Refunds view (flow + history).

**Architecture:** Browser → Frappe directly (`ErpNextService.ts` pattern, same as sales/stock/purchases). Four whitelisted methods in a new `awesome_dashboard.api.refunds` module; `Awesome Dashboard User` role gets POS Invoice perms via the existing `installer.py` mechanism. `pos_expenses` refund code is deleted after the new path ships.

**Tech Stack:** Frappe v16 / ERPNext v16 (Python), Vue 3 + TypeScript strict + @nuxt/ui 4 + Pinia + axios.

**Spec:** `docs/superpowers/specs/2026-11-13-refunds-view-design.md` (read both; the spec records the owner's decisions)

## Global Constraints

- Repos: `awesome_dashboard` at `~/Documents/code-projects/frappe/v16/apps/awesome_dashboard` (remote: Stelele/awesome_dashboard_scripts); `erpnext-dashboard` (cwd); `pos_expenses` at `~/Documents/code-projects/erpnext-point-of-sale-expenses`
- Both Frappe apps target ERPNext **version-16**
- Feature branches only; never push a default branch; merges need the owner's explicit instruction; address CodeRabbit comments before reporting a PR done (hivemind ADR 0049/0051/0052)
- TypeScript strict; **no non-null assertions** — widen the type instead
- Vue code style must match existing views/components (nuxt-ui components, DashboardLayout wrapper)
- Do not hand-roll return logic: ERPNext's `make_return_doc`, `is_invoice_returnable`, `get_invoice_item_returned_qty` (from `erpnext.controllers.sales_and_purchase_return`) are the mechanism
- Frontend build check is `npm run build:test` (vue-tsc) — run once at the end of frontend tasks, not per file

## Review Focus

Failure modes the spec implies that a person using this will hit, most likely first:

1. **Submitting a return qty greater than `returnable_qty`** (invoice already partially refunded) — expect a readable error, no partial/phantom return. Pinned by Task 1 smoke test (server throw) and Task 3 client max-qty clamp.
2. **Selecting an already-fully-returned or returned invoice in search** — expect it never appears in the refund search list. Pinned by Task 1's `is_invoice_returnable` post-filter step.
3. **Wrong company's invoices leaking into search or history** — expect empty results / company-scoped rows only. Pinned by Task 1 `company` filter assertions in both list endpoints.
4. **Double-clicking Refund creates two return invoices** — expect the submit button to be disabled while in flight. Pinned by Task 3 submitting-state step.
5. **Dashboard token user missing POS Invoice perms** — expect a readable "Not permitted" error surfaced in the UI, not a silent failure. Pinned by Task 3 error-handling step + Task 5 permissions smoke.

---

### Task 1: `awesome_dashboard` — `api/refunds.py` + POS Invoice permissions

**Files:**
- Create: `awesome_dashboard/api/refunds.py` (path relative to `~/Documents/code-projects/frappe/v16/apps/awesome_dashboard`)
- Modify: `awesome_dashboard/installer.py` — add one entry to `DASHBOARD_PERMISSIONS` (dict near the top of the file)
- Branch: `git switch -c feature/refunds-api` in the `awesome_dashboard` repo

**Interfaces:**
- Consumes: `erpnext.controllers.sales_and_purchase_return.{get_invoice_item_returned_qty, is_invoice_returnable, make_return_doc}` (exact import line in `pos_expenses/api.py:4-8`)
- Produces (whitelisted, `@frappe.whitelist(allow_guest=False)`, module `awesome_dashboard.api.refunds`):
  - `get_pos_invoices_for_refund(company, from_date=None, to_date=None, status=None, search_term=None, limit=200)` → list of dicts (keys: `name, customer, customer_name, status, grand_total, currency, posting_date, posting_time, paid_amount, total_qty, owner`), only returnable invoices
  - `get_invoice_detail_for_refund(invoice_name)` → dict with `items` (each incl. `returnable_qty`), `taxes`, `payments` (absolute values)
  - `process_pos_refund(invoice_name, return_items)` → `{"return_invoice": <name>}`
  - `get_refunds_for_period(company, from_date, to_date, limit=200)` → list of dicts (keys: `name, customer, customer_name, return_against, grand_total, currency, posting_date, posting_time, owner`), `is_return=1`, `docstatus=1`, newest first

- [ ] **Step 1: Create `refunds.py` with the two list endpoints**

Move `get_pos_invoices_for_refund` and `get_invoice_detail_for_refund` and `process_pos_refund` from `pos_expenses/api.py` (lines ~269-464), with these deltas:
- Every endpoint takes `company` first; `get_pos_invoices_for_refund` and `get_refunds_for_period` filter `tabPOS Invoice` by `company` (mirrors `finance.py` convention)
- `get_pos_invoices_for_refund` date filter becomes a range: `posting_date: ["between", [from_date, to_date]]` when both given (single-sided `>=`/`<=` when only one); `limit` default 200
- Keep `process_pos_refund`'s `json.loads(return_items)` string contract and all validation verbatim
- Keep docstring style of `stock.py` (`args:` block)

New `get_refunds_for_period`: `frappe.get_all("POS Invoice", filters={"docstatus": 1, "is_return": 1, "company": company, "posting_date": ["between", [from_date, to_date]]}, fields=[name, customer, customer_name, return_against, grand_total, currency, posting_date, posting_time, owner], order_by="posting_date desc, posting_time desc", limit=cint(limit))`.

- [ ] **Step 2: Syntax + import check**

Run: `cd ~/Documents/code-projects/frappe/v16/apps/awesome_dashboard && python3 -c "import ast; ast.parse(open('awesome_dashboard/api/refunds.py').read())"`
Expected: no output (parses clean). Full frappe import happens only inside the bench env — do not `import awesome_dashboard` with system python.

- [ ] **Step 3: Add POS Invoice to `DASHBOARD_PERMISSIONS`**

```python
"POS Invoice": {"read": 1, "write": 1, "create": 1, "submit": 1},
```

(inserted alphabetically between `"Payment Entry"` and `"Purchase Invoice"` entries)

- [ ] **Step 4: Bench smoke test on `development.localhost`**

Run: `bench --site development.localhost migrate` then as Administrator:
`bench --site development.localhost execute awesome_dashboard.api.refunds.get_refunds_for_period --kwargs "{'company': 'Njeremoto Enterprises', 'from_date': '2026-01-01', 'to_date': '2026-12-31'}"`
Expected: exits 0, prints `[]` or a list — proves module loads, SQL is valid. (Company name: use any company that exists on the site; adjust if different.)

- [ ] **Step 5: Commit**

```bash
git add awesome_dashboard/api/refunds.py awesome_dashboard/installer.py
git commit -m "feat: add refunds API and POS Invoice permissions for dashboard role"
```

### Task 2: Open the `awesome_dashboard` PR

- [ ] **Step 1: Push and open the PR**

```bash
cd ~/Documents/code-projects/frappe/v16/apps/awesome_dashboard
git push -u origin feature/refunds-api
gh pr create --title "Refunds API for dashboard + POS Invoice role permissions" --fill
```

- [ ] **Step 2: Address CodeRabbit**

Run `gh pr view <n> --comments` after review lands; fix or reply to each thread. Do not merge — merging waits for the owner's explicit instruction (ADR 0051).

### Task 3: Dashboard frontend — types + service methods

**Files:**
- Create: `frontend/src/types/Refunds.ts`
- Modify: `frontend/src/services/ErpNextService.ts` (add four public methods; add imports)
- Branch: `git switch -c feature/refunds-view` in `erpnext-dashboard`

**Interfaces:**
- Consumes: `awesome_dashboard.api.refunds.*` endpoints from Task 1 (signatures above); `useAuthStore().company` / `.url`; `ErpNextResponse<T>` unwrap (`{ message: T[] }`)
- Produces (used by Task 4):
  - Types in `Refunds.ts`: `RefundableInvoice`, `RefundInvoiceDetail` (has `items: RefundableItem[]`, `taxes`, `payments`; `RefundableItem` has `returnable_qty: number`), `RefundHistoryItem` (has `return_against: string | null`), `ReturnItemPayload` (`{ item_name: string; qty: number }`)
  - `ErpNextService.getPosInvoicesForRefund(params: { from_date?: string; to_date?: string; status?: string; search_term?: string; limit?: number }): Promise<RefundableInvoice[]>` — GET `/api/method/awesome_dashboard.api.refunds.get_pos_invoices_for_refund`, params plus `company: authStore.company`
  - `ErpNextService.getInvoiceDetailForRefund(invoiceName: string): Promise<RefundInvoiceDetail>` — GET `.../get_invoice_detail_for_refund`, param `invoice_name`
  - `ErpNextService.processPosRefund(invoiceName: string, returnItems: ReturnItemPayload[]): Promise<{ return_invoice: string }>` — POST `.../process_pos_refund`, body `{ invoice_name, return_items: JSON.stringify(returnItems) }` (string contract is deliberate — the Frappe method does `json.loads`)
  - `ErpNextService.getRefundsForPeriod(fromDate: string, toDate: string, limit = 200): Promise<RefundHistoryItem[]>` — GET `.../get_refunds_for_period`, params `from_date, to_date, limit, company`

- [ ] **Step 1: Write `types/Refunds.ts`**

Interfaces exactly as in Produces; number fields typed `number`, nullable `return_against` typed `string | null`.

- [ ] **Step 2: Add the four service methods**

Follow the `getSalesSummary` GET pattern (params + `ErpNextResponse<T>`) and the existing POST pattern used by purchase/stock mutations. Response of `process_pos_refund` is `{ message: { return_invoice: string } }`.

- [ ] **Step 3: Verify the build**

Run: `cd frontend && npm run build:test`
Expected: vue-tsc passes with the new code (methods unused is fine — vue-tsc still type-checks them).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/Refunds.ts frontend/src/services/ErpNextService.ts
git commit -m "feat: refunds API methods in ErpNextService"
```

### Task 4: Dashboard frontend — RefundsView, route, nav

**Files:**
- Create: `frontend/src/views/RefundsView.vue`
- Create: `frontend/src/components/NewRefundModal.vue`
- Modify: `frontend/src/routes/index.ts` (lazy import + `/refunds` route, `beforeEnter: authGuard`)
- Modify: `frontend/src/stores/NavStore.ts` (nav item: `{ label: "Refunds", icon: "i-lucide-undo-2", to: "/refunds" }` — after Expenses)

**Interfaces:**
- Consumes: Task 3 service methods; `DashboardLayout` (`@/layouts/DashboardLayout.vue`); global period from `useDataStore()` (`dataStore.dateRange.start/.end`, `dataStore.currentPeriod` — same global the sidebar PeriodSelector drives); nuxt-ui `UModal`, `UTable`-style components as used in `SalesTable.vue`/`OrderDetailsModal.vue`
- Produces: nothing downstream

- [ ] **Step 1: Build `RefundsView.vue`**

Layout: history table of `getRefundsForPeriod(dataStore.dateRange.start, dataStore.dateRange.end)` (columns: invoice name, return-against, customer, grand_total + currency, posting date/time), a "New Refund" button opening `NewRefundModal`, refetch on period change (watch `dataStore.dateRange`) and after a successful refund. Loading + empty states in the style of existing views.

- [ ] **Step 2: Build `NewRefundModal.vue`**

Flow (ported from `pos_expenses/pos_extension.js` refund modal, rebuilt with nuxt-ui):
1. Invoice search: date-range + status + search term inputs → `getPosInvoicesForRefund`; row click loads `getInvoiceDetailForRefund`
2. Per-item refund: number input per item clamped `0..returnable_qty` (client clamp — Review Focus #1)
3. Submit button disabled while a refund is in flight (`refunding.value`) — Review Focus #4
4. `processPosRefund(invoiceName, selectedItems)`; success shows the returned invoice name in a toast and closes the modal
5. Errors (incl. Frappe permission throws — Review Focus #5) surface as a visible error message in the modal, never swallowed

- [ ] **Step 3: Wire route + nav**

`/refunds` route with `authGuard`; nav entry in `NavStore.ts` per Files.

- [ ] **Step 4: Verify the build**

Run: `cd frontend && npm run build:test`
Expected: passes.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/views/RefundsView.vue frontend/src/components/NewRefundModal.vue frontend/src/routes/index.ts frontend/src/stores/NavStore.ts
git commit -m "feat: Refunds view with refund flow and refund history"
```

### Task 5: Dashboard PR

- [ ] **Step 1: Push, open PR, address CodeRabbit** (same protocol as Task 2; do not merge)

### Task 6: Delete refund from `pos_expenses`

**Files:**
- Modify: `pos_expenses/api.py` — delete `get_pos_invoices_for_refund`, `get_invoice_detail_for_refund`, `process_pos_refund` (lines ~269-464); delete now-unused imports `get_invoice_item_returned_qty`, `is_invoice_returnable`, `make_return_doc` and the top-level `import json` if only refund code used it (`get_partial_print_url` also uses `json.loads` — check before removing)
- Modify: `pos_expenses/public/js/pos_extension.js` — delete the Refund inner-button block (lines 29-32) and the refund functions block (`open_refund_invoices_modal` at line 533 through `_execute_refund`, ending just before the closing `};` / `callback();` at ~lines 1005-1006); reprint + expense code untouched
- Modify: `README.md` + any docs referencing refunds (feature bullets, screenshots)
- Branch: `git switch -c feature/remove-refund` in `erpnext-point-of-sale-expenses`

- [ ] **Step 1: Make the deletions**

- [ ] **Step 2: Verify nothing references the removed code**

Run:
```bash
cd ~/Documents/code-projects/erpnext-point-of-sale-expenses
grep -rni "refund" --include="*.py" --include="*.js" --include="*.md" .
python3 -c "import ast; ast.parse(open('pos_expenses/api.py').read())"
node --check pos_expenses/public/js/pos_extension.js
```
Expected: zero refund references (docs included); both files parse.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat!: remove refund functionality (moved to awesome_dashboard)"
```

- [ ] **Step 4: Push, open PR, address CodeRabbit** (do not merge)

### Task 7: End-to-end verification on the local bench

Prerequisite: Tasks 1-4 code present on the bench (awesome_dashboard branch installed on `development.localhost`).

- [ ] **Step 1: Migrate so the POS Invoice perms apply**

Run: `cd ~/Documents/code-projects/frappe/v16 && bench --site development.localhost migrate`
Expected: completes; `bench --site development.localhost execute awesome_dashboard.installer.ensure_dashboard_permissions` idempotent re-run exits 0.

- [ ] **Step 2: Full refund path smoke**

Submit (or pick an existing) POS Invoice for a small partial refund via the API as the dashboard user; then confirm `get_refunds_for_period` lists the new return with correct `return_against`; then confirm a second refund attempt of the same qty throws the returnable-qty error (Review Focus #1).

- [ ] **Step 3: POS page has no refund button**

Open the POS page as a POS user; expect only the remaining inner buttons (New Invoice, Reprint Invoices, etc.) — no Refund.

## Notes for executors

- Frappe whitelisted method responses arrive wrapped: `{ message: <payload> }` — that is what `ErpNextResponse<T>` unwraps.
- `get_partial_print_url`/`show_partial_print` and the expense methods in `pos_expenses/api.py` stay; only the refund trio goes.
- The dashboard's global period (`DataStore`) is the single source for the history range — do not add a second period selector on the Refunds view.
