# Finance Manager

Finance Manager is a personal finance application focused on operational clarity, reliable transaction flows, and a premium dark interface.

This repository contains only the source code and infrastructure needed to run the project locally.
Internal study notes, planning documents, transition logs, and other private working materials are intentionally excluded from the public repository.

## Documentation Map

Read the public documentation in this order:

1. `README.md`
2. `spec/Roadmap/project-roadmap-spec.md`
3. `spec/Roadmap/phase-0-foundation-spec.md`
4. `spec/Roadmap/phase-2-operational-finance-spec.md`
5. `spec/Roadmap/phase-3-credit-and-invoice-spec.md`
6. `spec/Roadmap/phase-4-financial-planning-spec.md`
7. `spec/Roadmap/phase-5-control-and-governance-spec.md`
8. `spec/Roadmap/phase-6-design-evolution-spec.md`
9. `spec/Roadmap/phase-7-intelligence-and-finalization-spec.md`
10. `spec/Financial/phase-2-orchestration-roadmap.md`
11. `spec/Financial/financial-accounts-spec.md`
12. `spec/Financial/transaction-categories-spec.md`
13. `spec/Financial/transactions-core-spec.md`
14. `spec/Financial/financial-overview-spec.md`
15. `spec/Financial/credit-cards-spec.md`
16. `spec/Financial/scheduled-entries-spec.md`

The roadmap specs are the source of truth for:

- project phases
- global sequence
- current status by phase
- next module decision logic

The module specs remain the source of truth for detailed implementation contracts.

## Current Scope

The project currently includes:

- backend authentication with register, login, and current-user flow
- frontend authentication integrated with the real backend
- financial accounts module with create, list, controlled edit, and governed inactivation flows
- transaction categories module with create, list, controlled edit, and governed inactivation flows
- basic backend audit trail for governed mutations in financial accounts and transaction categories
- lightweight governance reading in `Settings`, with filtered audit-log consultation for governed mutations
- transactions core with income, expense, transfer, period listing, and account/category integration
- financial overview dashboard with consolidated reading of balances and recent movements
- credit cards with create and list flows
- invoices with automatic creation by cycle, manual opening, advanced closing, and detailed reading
- credit card purchases with installment distribution across future invoice cycles
- partial and total invoice payment with editable real amount
- controlled invoice adjustments with credit, discount, fees, interest, penalty, and manual correction
- authenticated application shell shared between protected pages
- automated backend coverage for operational finance and advanced credit flows
## Current Project State

The project is currently here:

- `Fase 0` complete for current scope
- `Fase 2` complete for current scope
- `Fase 3` complete for current scope
- `Fase 4` complete for current scope
- `Fase 5` complete for current scope
- `Fase 6` complete for current scope
- `Fase 7` complete for current scope
- credit and invoice advanced core implemented and validated for current scope
- `Scheduled Entries / Planned Transactions` implemented and validated as the first module of `Fase 4`
- planning now reads recurring commitments by visible occurrences per competence, preserving treated history and future competences in the same recurrence
- backend now applies pending EF Core migrations on API startup to keep planning support structures aligned with the running database
- simple financial calendar now uses the same occurrence-based operational model, preserving navigation by week/month without losing the visible planning window
- basic operational forecasting now highlights projected balance, expected income, expected expenses, monthly concentration, and treatment rhythm on top of the stabilized occurrence and calendar base
- monthly decision-reading now highlights critical months, load distribution, and operational priorities without opening complex predictive models
- `Fase 4 - Financial Planning` is now closed as complete for the current scope
- first governance slice consolidated: controlled edit and inactivation for `Financial Accounts` and `Transaction Categories`
- basic audit trail is now opened in backend for controlled create, update, and inactivation events in `Financial Accounts` and `Transaction Categories`
- extra consistency protections are now implemented for current scope: they block inactivation of accounts and categories linked to active planned entries, and block invoice payment with inactive account
- explicit governance reading is now available through basic audit-log consultation in `Settings`
- governance reading now supports richer lightweight filters by entity, action, period, entity id, text search, and result limit
- `Fase 5 - Control and Governance` is now closed as complete for the current scope
- `Fase 6 - Design Evolution` is now closed as complete for the current scope
- `Fase 6` now also includes a shared visual layer for loading, empty, and error states, already applied to `Transactions` and `Invoices` as the first dense-screen refinement slice
- `Scheduled Entries` has now joined that Phase 6 refinement track, with shared states and calmer hierarchy across forecast, radar, calendar, and list areas
- `Credit Cards` now follows the same Phase 6 refinement track, with shared states, less compressed summary reading, and a calmer card statement hierarchy
- `Financial Accounts` and `Transaction Categories` now follow the same Phase 6 structural cleanup track, aligning shared states and reducing visual pressure in their summary and list layers before the backoffice refinement in `Settings`
- `Settings` now follows the same Phase 6 backoffice refinement track, with shared states, calmer filter grouping, and a more consistent audit-reading surface
- broader end-to-end screen refinement remains intentionally deferred to a final stage guided by explicit visual designs
- `Fase 7 - Intelligence and Finalization` is now open, starting from the dashboard with a first managerial-reading slice on top of the existing financial overview
- the Phase 7 dashboard slice now also exposes lightweight managerial cuts by account and by category, keeping the reading explainable and operational
- the Phase 7 dashboard slice now also compares the current period with an equivalent previous period, highlighting changes in income, expense, and operating result without turning the overview into heavy BI
- `Fase 7 - Intelligence and Finalization` is now closed as complete for the current scope
- the project can now be treated as concluded for the planned public roadmap scope, with final broad visual redesign remaining intentionally outside this closure
## Tech Stack

- Frontend: Next.js 16, React 19, TypeScript
- Backend: ASP.NET Core 8
- Database: PostgreSQL 16
- Infrastructure: Docker Compose

## Repository Structure

- `backend/`: ASP.NET Core solution and projects
- `frontend/`: Next.js application
- `infra/`: local infrastructure setup
- `spec/`: public-facing functional specifications, phase specs and roadmap

## Running Locally

### 1. Start PostgreSQL

From the repository root:

```powershell
cd infra
docker compose up -d
```

### 2. Run the backend

In a new terminal:

```powershell
cd backend/src/FinanceManager.Api
dotnet run
```

Backend default URL:

- `http://localhost:5004`

Swagger:

- `http://localhost:5004/swagger/index.html`

### 3. Run the frontend

In another terminal:

```powershell
cd frontend
npm install
npm run dev
```

Frontend default URL:

- `http://localhost:3000`

## Main Routes

- `/login`
- `/register`
- `/dashboard`
- `/financial-accounts`
- `/transaction-categories`
- `/transactions`
- `/credit-cards`
- `/invoices`
## Validation Status

Recent local validation includes:

- `dotnet test FinanceManager.sln`
- `npm run lint`
- `npm run build`
- manual end-to-end verification of accounts, categories, transactions, dashboard, cards, invoices, invoice payments, and invoice adjustments

Readiness notes for current scope:

- the product is operationally complete through the planned arc from `Fase 0` to `Fase 7`
- the dashboard now offers lightweight managerial reading by account, category, and equivalent previous period
- broader full-screen visual redesign remains intentionally deferred to a final design-guided pass
- some backend validations may require isolated output paths when a local `FinanceManager.Api` process is locking build artifacts during test execution
- heavier reporting was intentionally not opened yet, because the current priority is controlled closure and maintainability
## Notes

- Development uses local PostgreSQL through Docker.
- Frontend and backend are configured for local integration.
- This public repository is intentionally kept focused on executable code and selected specs.

## License

This project currently has no license file attached.
All rights reserved unless a license is added later.


