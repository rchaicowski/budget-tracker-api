# Budget Tracker API

A RESTful backend API built with **ASP.NET Core** and **PostgreSQL** for managing personal finances — accounts, transactions, categories, and (soon) budgets — with JWT authentication and per-user data isolation.

---

## Tech Stack

* **Framework:** .NET 10 / C#
* **Database:** PostgreSQL, via Entity Framework Core (Npgsql provider)
* **Authentication:** JWT Bearer tokens, passwords hashed with BCrypt
* **Logging:** Serilog (structured console logging, plus request logging)
* **Testing:** xUnit + EF Core InMemory provider
* **API Docs:** Swagger / OpenAPI (Swashbuckle), with Bearer auth support in the UI
* **Containerization:** Docker, Docker Compose (multi-stage build for a lean runtime image)
* **Version Control:** Git & GitHub

---

## Database Schema

* `users`: User accounts — username, email, BCrypt password hash.
* `accounts`: Financial accounts (Checking, Savings, Credit Card) linked to a user. Stores an **opening balance**; current balance is computed from transactions at query time.
* `categories`: Shared category list (Groceries, Rent, Utilities, Salary, Entertainment, Other), seeded via EF Core migrations.
* `transactions`: Individual income/expense entries, linked to an account and a category.
* `budgets`: Per-category monthly spending limits, linked to a user. *(Schema exists; API endpoints not yet implemented — coming in a later phase.)*

> Schema is managed through EF Core migrations (`Migrations/` folder) rather than a hand-maintained SQL file, so migrations are the source of truth for the current schema.

---

## Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) 10.0 or later
* [PostgreSQL](https://www.postgresql.org/) running locally, **or** [Docker](https://www.docker.com/) + Docker Compose (see below for the fastest path)
* [Git](https://git-scm.com/)

### Option A: Docker Compose (fastest)

Spins up the API and a Postgres instance together, no local Postgres install needed:

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`, with Swagger UI at `http://localhost:8080/swagger`. Data persists across restarts in a named Docker volume; run `docker compose down -v` to reset it completely.

> **Note:** `docker-compose.yml` uses its own database (`budgettracker`) and hardcoded local-only credentials for convenience — this is intentional for a throwaway local dev database, but is **not** how secrets are handled for the non-Docker setup below (which uses `dotnet user-secrets`), and won't be how this is done once the project deploys to AWS. Don't reuse these credentials anywhere real.

### Option B: Local Postgres + `dotnet run`

If you'd rather run everything locally without Docker:

1. Clone the repo and restore dependencies:
   ```bash
   git clone <repo-url>
   cd BudgetTrackerApi
   dotnet restore
   ```
2. Create a Postgres database and a dedicated app user (don't use the `postgres` superuser role):
   ```sql
   CREATE DATABASE budget_tracker_dev;
   CREATE USER budget_tracker_app WITH PASSWORD 'your-password-here';
   GRANT ALL PRIVILEGES ON DATABASE budget_tracker_dev TO budget_tracker_app;
   ```
   Then, connected to `budget_tracker_dev` specifically, grant table/sequence access:
   ```sql
   GRANT USAGE ON SCHEMA public TO budget_tracker_app;
   GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO budget_tracker_app;
   GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO budget_tracker_app;
   ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO budget_tracker_app;
   ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO budget_tracker_app;
   ```
3. Configure secrets locally (never committed — see `appsettings.json.example` for the expected shape):
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=budget_tracker_dev;Username=budget_tracker_app;Password=your-password-here"
   dotnet user-secrets set "Jwt:Key" "a-long-random-secret-at-least-32-characters"
   ```
4. Apply migrations:
   ```bash
   dotnet ef database update
   ```
5. Run the API:
   ```bash
   dotnet run
   ```
   Swagger UI is available at `/swagger` in development.

### Running Tests

```bash
cd BudgetTrackerApi.Tests
dotnet test
```

---

## Authentication

Most endpoints require a JWT. To authenticate:

1. `POST /api/Users` to register, or `POST /api/Users/login` if you already have an account.
2. Copy the `token` field from the response.
3. In Swagger UI, click **Authorize** (top right) and enter `Bearer {token}`.
4. Authenticated requests are now scoped to that user — accounts and transactions are automatically filtered to the logged-in user, and attempting to access or modify another user's data returns `404`/`400` rather than exposing it.

---

## API Endpoints

### Users (`/api/Users`)
* `GET /api/Users` — Retrieve all users *(public — for demo purposes; no sensitive data returned)*
* `GET /api/Users/{id}` — Retrieve a single user by ID *(public)*
* `POST /api/Users` — Register a new user
* `POST /api/Users/login` — Authenticate and receive a JWT

### Accounts (`/api/Accounts`) — requires authentication
* `GET /api/Accounts` — List the current user's accounts, with computed current balance
* `GET /api/Accounts/{id}` — Retrieve one of the current user's accounts by ID
* `POST /api/Accounts` — Create a new account for the current user

### Transactions (`/api/Transactions`) — requires authentication
* `GET /api/Transactions` — List transactions on accounts owned by the current user
* `GET /api/Transactions/{id}` — Retrieve a single transaction (must belong to the current user)
* `POST /api/Transactions` — Create a transaction (requires a valid `accountId` owned by the current user and a valid `categoryId`)
* `PUT /api/Transactions/{id}` — Update a transaction (ownership re-validated on the new account too)
* `DELETE /api/Transactions/{id}` — Delete a transaction

---

## Security Notes

* Passwords are hashed with BCrypt; plaintext passwords are never stored or logged.
* All account/transaction endpoints validate that the resource belongs to the authenticated user before returning or modifying it, preventing IDOR (Insecure Direct Object Reference) access to other users' data.
* Secrets (DB credentials, JWT signing key) are kept out of source control via `dotnet user-secrets` locally; `appsettings.json` in the repo contains placeholder values only.

---

## Roadmap

* [ ] Budgets endpoints (monthly limits per category, spend-vs-budget comparison)
* [ ] Monthly reports/aggregation endpoint
* [ ] Filtering & pagination on transaction list
* [ ] Redis caching
* [ ] Rate limiting on auth endpoints
* [ ] Receipt attachments (S3)
* [ ] Recurring transactions worker (Python)
* [x] Dockerized local dev environment
* [ ] CI/CD via GitHub Actions
* [ ] AWS deployment
