# MoneyManager

A personal financial management web application designed as a Mint.com replacement.

## Features

- **Transaction Management** — Import, categorize, and track financial transactions
- **Multi-Bank Support** — Import from Mint.com, RBC, and CIBC CSV exports
- **Smart Categorization** — Auto-categorize transactions with customizable rules
- **Account Management** — Track multiple accounts with alternative name matching
- **Financial Visualizations** — Interactive charts for spending, income, net income, and trends
- **AI-Powered Insights** — Personalized financial analysis via pluggable OpenAI-compatible providers
- **Budgets** — Track spending against budget targets

## Technology Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core Minimal APIs (.NET 10) |
| Frontend | React 19 + TypeScript, Vite 8 |
| Styling | Tailwind CSS v4 |
| State | TanStack Query v5, React Router v7 |
| Charts | ECharts |
| Database | SQLite + Entity Framework Core 10 |
| Dev orchestration | .NET Aspire (optional) |
| AI | OpenAI-compatible chat completions |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

### Run (two terminals)

```bash
# Terminal 1 — API
dotnet run --project src/MoneyManager.Api --urls http://localhost:5000

# Terminal 2 — Frontend
cd src/moneymanager-web
npm install
npm run dev          # Vite on :5173, proxies /api → :5000
```

Or use Aspire to start everything at once:

```bash
dotnet run --project src/MoneyManager.AppHost
```

### Docker

```bash
docker compose up --build
```

The container serves the built SPA and the API on a single port.

## Project Structure

```
MoneyManager/
├── src/
│   ├── MoneyManager.Api/          # ASP.NET Core Minimal API
│   │   ├── Endpoints/             # Route groups (Transactions, Accounts, Categories, …)
│   │   ├── Services/              # DataService, TransactionService, AIService, …
│   │   ├── Data/                  # EF entities + DataContext + SQLite DB
│   │   └── Model/                 # Request/response DTOs
│   ├── moneymanager-web/          # React + Vite SPA
│   │   └── src/
│   │       ├── pages/             # Route pages
│   │       ├── components/        # Shared + ui design system
│   │       ├── hooks/             # TanStack Query hooks per resource
│   │       └── api/               # Axios client
│   ├── MoneyManager.Api.Tests/    # xUnit + FluentAssertions + NSubstitute
│   ├── MoneyManager.AppHost/      # Aspire dev orchestrator (dev only)
│   └── MoneyManager.ServiceDefaults/  # Shared OpenTelemetry + health checks
├── data/                          # SQLite DB (dev, gitignored)
├── docs/                          # ADRs, agent config
└── legacy/                        # Retired Blazor Hybrid desktop app
```

## Supported Import Formats

| Source | Format | Fields |
|--------|--------|--------|
| Mint.com | CSV | Date, Description, Amount, Category, Account |
| RBC | CSV | Account Type, Date, Description, Amount |
| CIBC | CSV | Date, Description, Debit, Credit |

## Auto-Categorization Rules

Rules match transaction descriptions using four comparison types:
- **Contains** — Description contains the pattern
- **Starts With** — Description starts with the pattern
- **Ends With** — Description ends with the pattern
- **Equals** — Description exactly matches the pattern

Rules can also transform the transaction description when applied.

## Configuration

Application settings in `appsettings.json` / `appsettings.Development.json`:
- Connection strings (SQLite path)
- Serilog logging

AI provider configuration (base URL, model, API key) is stored in the database and managed through the Settings page.

## License

Private project.
