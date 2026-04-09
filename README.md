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
- financial accounts module with create and list flows
- transaction categories module with create and list flows
- transactions core with income, expense, transfer, period listing, and account/category integration
- financial overview dashboard with consolidated reading of balances and recent movements
- authenticated application shell shared between protected pages
- automated backend coverage for transactions and financial overview flows

## Current Project State

The project is currently here:

- `Fase 0` complete for current scope
- `Fase 2` complete for current scope
- `Financial Accounts`, `Transaction Categories`, `Transactions Core` and `Financial Overview` complete for current scope
- next active phase: `Fase 3 - Credit and Invoice`

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

## Validation Status

Recent local validation includes:

- `dotnet test FinanceManager.sln`
- `npm run lint`
- `npm run build`
- manual end-to-end verification of accounts, categories, transactions and dashboard

## Notes

- Development uses local PostgreSQL through Docker.
- Frontend and backend are configured for local integration.
- This public repository is intentionally kept focused on executable code and selected specs.

## License

This project currently has no license file attached.
All rights reserved unless a license is added later.
