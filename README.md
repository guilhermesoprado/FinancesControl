# Finance Manager

Finance Manager is a personal finance application focused on operational clarity, reliable transaction flows, and a premium dark interface.

This repository contains only the source code and infrastructure needed to run the project locally.
Internal study notes, planning documents, transition logs, and other private working materials are intentionally excluded from the public repository.

## Current Scope

The project currently includes:

- backend authentication with register, login, and current-user flow
- frontend authentication integrated with the real backend
- financial accounts module with create and list flows
- transaction categories module with create and list flows
- authenticated application shell shared between protected pages

The next planned module is `Transactions Core`.

## Tech Stack

- Frontend: Next.js 16, React 19, TypeScript
- Backend: ASP.NET Core 8
- Database: PostgreSQL 16
- Infrastructure: Docker Compose

## Repository Structure

- `backend/`: ASP.NET Core solution and projects
- `frontend/`: Next.js application
- `infra/`: local infrastructure setup
- `spec/`: public-facing functional specifications for implemented modules

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
- `/financial-accounts`
- `/transaction-categories`

## Notes

- Development uses local PostgreSQL through Docker.
- Frontend and backend are configured for local integration.
- This public repository is intentionally kept focused on executable code and selected specs.

## License

This project currently has no license file attached.
All rights reserved unless a license is added later.
