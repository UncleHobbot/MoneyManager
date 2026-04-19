# Future Improvements

Suggested enhancements for MoneyManager — the ASP.NET 10 API + React web application. Each item includes a brief description, sketch acceptance criteria, and effort estimate.

**Effort key:** **S** = Small (1–3 days) · **M** = Medium (3–10 days) · **L** = Large (10+ days)

---

## 1. Budget Tracking · `L`

Set monthly spending budgets per category and track actuals against them.

**Description:**
Users define a monthly dollar amount for each category (e.g., Groceries $500/mo). A dashboard widget shows progress bars per category — green under budget, yellow near limit, red over. Supports rollover and per-period adjustments.

**Acceptance Criteria:**
- [ ] New `Budgets` table (`id`, `categoryId`, `monthlyAmount`, `effectiveFrom`, `effectiveTo`)
- [ ] API endpoints: CRUD for budgets, GET spending-vs-budget summary for a given month
- [ ] React dashboard card showing each budgeted category with a progress bar
- [ ] Alert banner when a category exceeds 90% of its budget
- [ ] Budget history view showing month-over-month adherence

---

## 2. Recurring Transaction Detection · `M`

Automatically identify subscriptions and recurring charges from transaction history.

**Description:**
Analyze transactions to detect patterns — same payee, similar amount, regular interval (weekly, monthly, yearly). Surface detected subscriptions in a dedicated panel so users can review, confirm, or dismiss them.

**Acceptance Criteria:**
- [ ] Detection algorithm scanning last 6–12 months of transactions for repeating payee + amount patterns
- [ ] New `RecurringTransactions` table storing confirmed subscriptions
- [ ] API endpoint returning detected and confirmed recurring items
- [ ] React panel listing subscriptions with frequency, amount, next expected date
- [ ] User can confirm, edit, or dismiss detected patterns

---

## 3. Net Worth Tracking · `M`

Track account balances over time and chart net worth trends.

**Description:**
Leverage the existing `Balances` table to build a time-series chart of total net worth (assets minus liabilities). Allow users to manually log or import balance snapshots. Show per-account contribution.

**Acceptance Criteria:**
- [ ] API endpoint returning net worth series (date, total, per-account breakdown)
- [ ] ApexCharts area/line chart on a dedicated Net Worth page
- [ ] Ability to manually add a balance snapshot for any account and date
- [ ] Distinguish asset accounts vs. liability accounts (credit cards, loans)
- [ ] Summary card: current net worth, 30-day change, YTD change

---

## 4. Transaction Search · `S`

Full-text search across transaction descriptions, amounts, and dates.

**Description:**
Add a global search bar that queries transactions by description substring, exact or range amounts, and date ranges. Uses SQLite FTS5 for performant text matching.

**Acceptance Criteria:**
- [ ] SQLite FTS5 virtual table over transaction descriptions
- [ ] API endpoint accepting query string, amount range, date range filters
- [ ] React search bar with instant results (debounced 300ms)
- [ ] Results highlight matching text
- [ ] Search from any page via a keyboard shortcut (Ctrl+K)

---

## 5. Bulk Categorization · `S`

Select multiple transactions and assign a category in one action.

**Description:**
Checkbox multi-select in the transaction list. A toolbar appears with bulk actions: assign category, delete, or apply rules to selected items.

**Acceptance Criteria:**
- [ ] Checkbox column in the transaction data grid
- [ ] Floating action bar showing count of selected items
- [ ] Bulk assign category via dropdown (single API call with array of IDs)
- [ ] Bulk delete with confirmation dialog
- [ ] Select-all / deselect-all toggle

---

## 6. Split Transactions · `M`

Split a single transaction across multiple categories.

**Description:**
A $150 Costco purchase might be $100 Groceries + $50 Household. Users split one transaction into child rows, each with its own category and amount, summing to the original.

**Acceptance Criteria:**
- [ ] New `TransactionSplits` table (`id`, `parentTransactionId`, `categoryId`, `amount`, `note`)
- [ ] API endpoints: create split, update split, delete split, get splits for transaction
- [ ] React split editor dialog with dynamic row add/remove
- [ ] Validation: split amounts must equal original transaction amount
- [ ] Charts and reports respect splits (allocate to sub-categories)

---

## 7. Custom Date Ranges · `S`

Date picker for charts instead of only preset periods (m1, y1, 12, w, a).

**Description:**
Add a date-range picker alongside the existing period buttons. Users select arbitrary start/end dates for any chart or report view.

**Acceptance Criteria:**
- [ ] Date range picker component (start date, end date)
- [ ] Preset buttons remain as quick-select shortcuts
- [ ] All chart API endpoints accept optional `startDate` / `endDate` query params
- [ ] Selected range persists during navigation between chart sub-pages
- [ ] URL query parameters reflect the chosen range for shareability

---

## 8. Export & Reporting · `M`

Generate PDF reports and Excel exports for custom date ranges.

**Description:**
Users export filtered transaction data to CSV/Excel or generate a formatted PDF summary report. Reports include spending by category, income vs. expenses, and account summaries.

**Acceptance Criteria:**
- [ ] CSV export of any filtered transaction view (existing, enhance)
- [ ] Excel (.xlsx) export with formatted headers and category subtotals
- [ ] PDF report generation (monthly summary, annual summary) via a server-side library
- [ ] Date range and account filters applied to exports
- [ ] Download triggered from a toolbar button; large reports generated async

---

## 9. Multi-currency Support · `L`

Handle USD and CAD transactions with exchange rate conversion.

**Description:**
Accounts are tagged with a currency. Transactions in foreign currencies are stored at their original amount plus the exchange rate at time of import. Reporting converts to a base currency using stored or fetched rates.

**Acceptance Criteria:**
- [ ] `Currency` column on `Accounts` table (default CAD)
- [ ] `ExchangeRate` and `OriginalAmount` columns on `Transactions`
- [ ] Optional integration with a free FX rate API (e.g., Bank of Canada daily rates)
- [ ] Charts normalize to base currency; tooltip shows original amount
- [ ] Settings page to choose base display currency

---

## 10. Notifications & Alerts · `M`

Alert users to large transactions, unusual spending, and budget breaches.

**Description:**
Configurable alert rules: transaction over $X, category spending spike vs. average, new payee, duplicate transaction warning. Alerts surface as in-app notifications and optionally via email/webhook.

**Acceptance Criteria:**
- [ ] `AlertRules` table (type, threshold, categoryId, enabled)
- [ ] Background job evaluating rules after each import
- [ ] In-app notification bell with unread count
- [ ] Alert detail panel with dismiss / snooze actions
- [ ] Optional webhook or email integration for external notifications

---

## 11. Mobile Responsive & PWA · `L`

Progressive Web App support with mobile-optimized transaction entry.

**Description:**
Make the React frontend installable as a PWA. Optimize layouts for mobile — collapsible sidebar, swipe gestures on transaction rows, quick-add FAB for new transactions.

**Acceptance Criteria:**
- [ ] Valid `manifest.json` and service worker for PWA install prompt
- [ ] Responsive breakpoints: mobile (<768px), tablet, desktop
- [ ] Bottom navigation bar on mobile replacing the sidebar
- [ ] Swipe-to-categorize gesture on transaction rows
- [ ] Offline read support for cached transaction data

---

## 12. Additional Bank Imports · `S` per bank

Add CSV import parsers for TD, BMO, and Scotiabank.

**Description:**
Follow the existing bank import pattern (`TransactionService.{Bank}.cs` partial class) to add parsers for additional Canadian banks. Each parser maps the bank's CSV columns to the transaction model and handles duplicate detection.

**Acceptance Criteria:**
- [ ] `TransactionService.TD.cs` — parse TD CSV format
- [ ] `TransactionService.BMO.cs` — parse BMO CSV format
- [ ] `TransactionService.Scotiabank.cs` — parse Scotiabank CSV format
- [ ] Account matching via `Account.Number` and `AlternativeName` fields
- [ ] Duplicate detection consistent with existing import logic
- [ ] Unit tests for each parser covering edge cases (empty rows, special characters)

---

## 13. Receipt & Document Attachment · `M`

Attach photos or PDFs to transactions for record-keeping.

**Description:**
Users upload receipt images or PDF documents linked to a transaction. Files stored on disk (or object storage in future). Thumbnails shown in transaction detail view.

**Acceptance Criteria:**
- [ ] `Attachments` table (`id`, `transactionId`, `fileName`, `filePath`, `mimeType`, `uploadedAt`)
- [ ] API endpoint for upload (multipart form), download, and delete
- [ ] Max file size limit (e.g., 10 MB) with validation
- [ ] Thumbnail preview for images; PDF icon for documents
- [ ] Docker volume mount for persistent attachment storage

---

## 14. Data Visualization Enhancements · `M`

Heatmap calendar, trend lines, and year-over-year comparison charts.

**Description:**
Expand the charting suite beyond current pie/bar charts. Add a GitHub-style heatmap calendar showing daily spending intensity, trend/regression lines on time series, and YoY comparison overlays.

**Acceptance Criteria:**
- [ ] Heatmap calendar component (365-day grid, color intensity by daily spend)
- [ ] Trend line overlay option on existing line/area charts
- [ ] Year-over-year comparison: select two periods, overlay on same axes
- [ ] Category drill-down: click a chart segment to see underlying transactions
- [ ] Consistent color palette across all chart types

---

## 15. User Authentication · `M`

Optional login for shared or NAS-hosted deployments.

**Description:**
Add opt-in authentication so multiple users on a shared network can each have their own data. Disabled by default for single-user local installs. Uses ASP.NET Identity with JWT tokens.

**Acceptance Criteria:**
- [ ] Feature flag in `appsettings.json` (`Authentication:Enabled`, default `false`)
- [ ] ASP.NET Identity with SQLite provider; JWT bearer tokens for the API
- [ ] React login page; token stored in `httpOnly` cookie or `localStorage`
- [ ] Per-user data isolation (all queries scoped by `userId`)
- [ ] Admin user auto-created on first run when auth is enabled

---

## 16. API Rate Limiting · `S`

Protect API endpoints from abuse on exposed deployments.

**Description:**
Apply rate limiting middleware to the ASP.NET API. Configurable limits per endpoint group (e.g., 100 req/min for reads, 30 req/min for writes). Uses the built-in .NET `RateLimiter` middleware.

**Acceptance Criteria:**
- [ ] `Microsoft.AspNetCore.RateLimiting` middleware configured in `Program.cs`
- [ ] Sliding window policy: configurable via `appsettings.json`
- [ ] Different limits for read vs. write endpoints
- [ ] `429 Too Many Requests` response with `Retry-After` header
- [ ] Rate limit headers in responses (`X-RateLimit-Remaining`, `X-RateLimit-Reset`)

---

## Priority Matrix

| # | Improvement | Effort | Impact | Suggested Phase |
|---|------------|--------|--------|-----------------|
| 1 | Budget Tracking | L | High | Phase 2 |
| 2 | Recurring Transaction Detection | M | High | Phase 2 |
| 3 | Net Worth Tracking | M | High | Phase 2 |
| 4 | Transaction Search | S | High | Phase 1 |
| 5 | Bulk Categorization | S | High | Phase 1 |
| 6 | Split Transactions | M | Medium | Phase 2 |
| 7 | Custom Date Ranges | S | Medium | Phase 1 |
| 8 | Export & Reporting | M | Medium | Phase 2 |
| 9 | Multi-currency Support | L | Medium | Phase 3 |
| 10 | Notifications & Alerts | M | Medium | Phase 3 |
| 11 | Mobile Responsive & PWA | L | Medium | Phase 3 |
| 12 | Additional Bank Imports | S each | Medium | Phase 1 |
| 13 | Receipt & Document Attachment | M | Low | Phase 3 |
| 14 | Data Visualization Enhancements | M | Medium | Phase 2 |
| 15 | User Authentication | M | Low | Phase 3 |
| 16 | API Rate Limiting | S | Low | Phase 1 |

**Phase 1** — Quick wins, high value, small effort
**Phase 2** — Core feature expansion
**Phase 3** — Polish, scale, and optional features
