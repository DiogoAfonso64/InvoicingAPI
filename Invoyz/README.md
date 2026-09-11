# Invoyz

A small invoice management app: customers, products, invoices with line items,
and PDF generation for any invoice.

- **`server/`** — ASP.NET Core (.NET 8) Web API, built with a Clean
  Architecture layering (Domain / Application / Infrastructure / Controllers).
  Data is held in an in-memory cache that stands in for a real database — it's
  re-seeded with sample customers, products, and invoices every time the API
  starts, and nothing is persisted between runs.
- **`client/`** — Vue 3 + Vite frontend (Vue Router, Tailwind CSS, Axios).

## Features

- Full CRUD for customers, products, and invoices (with line items) from the
  UI, backed by REST endpoints (GET/POST/PUT/PATCH/DELETE).
- Invoice totals and tax are calculated server-side (`InvoiceCalculator`, using
  `decimal` arithmetic) — the client never has to be trusted for the numbers
  that get persisted.
- Per-invoice PDF generation: clicking *Print* on an invoice starts a
  background job (customer details, line items, and totals rendered via
  QuestPDF), which the frontend polls until it's ready and then downloads.
- Health check at `/health` and a live Swagger UI for exploring the API.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js LTS](https://nodejs.org/) (includes npm)

Check both are installed:

```powershell
dotnet --version
node --version
npm --version
```

## Project structure

```
server/   ASP.NET Core Web API (Invoyz.Api)
client/   Vue 3 + Vite frontend
tests/    xUnit unit + integration tests for the API
docs/     Architecture diagrams (PlantUML) and an OpenAPI/Swagger snapshot
Invoyz.sln  Solution file tying server/ and tests/ together
```

## Running the app

You need both the API and the frontend running at the same time — the
frontend calls the API directly (CORS is already configured for this).

**API** (from the repo root, or `cd server` first):

```powershell
dotnet run --project server\Invoyz.Api.csproj
```

The API starts on `http://localhost:5080`. On startup it seeds itself with
sample data automatically. Useful URLs once it's running:

- `http://localhost:5080/health` — health check
- `http://localhost:5080/swagger` — interactive API docs

**Frontend** (in a second terminal):

```powershell
cd client
npm install   # first time only
npm run dev
```

The frontend starts on `http://localhost:5173` and talks to the API at
`http://localhost:5080` by default (configurable via `client/.env`,
`VITE_API_BASE_URL`).

## Running the tests

**Backend** (unit tests mock their dependencies; integration tests exercise
the real HTTP pipeline end-to-end via `WebApplicationFactory`):

```powershell
dotnet test Invoyz.sln
```

**Frontend** (calculation logic — `client/src/utils/invoiceTotals.js`):

```powershell
cd client
npm test
```

## Building for production

```powershell
dotnet build server\Invoyz.Api.csproj

cd client
npm run build   # outputs to client/dist
```

## Docs

`docs/` has a component diagram and a domain model diagram (PlantUML —
open with a PlantUML viewer/extension) plus a `swagger.json` snapshot of the
API surface.
