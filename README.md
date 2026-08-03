# Budget Tracker API

A RESTful backend API built with **.NET Core** and **PostgreSQL** for managing personal finances, tracking accounts, logging transactions, and setting monthly budgets.

---

## Tech Stack

* **Framework:** .NET 8 / C#
* **Database:** PostgreSQL
* **Database Tooling:** DBeaver / Entity Framework Core (or Dapper)
* **Version Control:** Git & GitHub

---

## Database Schema

The database consists of four core tables:

* `users`: Stores user profile and authentication information.
* `accounts`: Stores financial accounts (e.g., Checking, Savings) linked to a user.
* `transactions`: Logs individual income and expense entries tied to an account.
* `budgets`: Tracks spending limits per category for a user.

> The initial raw SQL migration script can be found in [`schema.sql`](./schema.sql).

---

## Getting Started

### Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or later)
* [PostgreSQL](https://www.postgresql.org/) database instance running locally or via Docker
* [Git](https://git-scm.com/)

## API Endpoints

### Users (`/api/Users`)
* `GET /api/Users` - Retrieve all users
* `GET /api/Users/{id}` - Retrieve a single user by ID
* `POST /api/Users` - Create a new user

### Transactions (`/api/Transactions`)
* `GET /api/Transactions` - Retrieve all transactions
* `GET /api/Transactions/{id}` - Retrieve a transaction by ID
* `POST /api/Transactions` - Create a transaction (requires valid `accountId`)
* `PUT /api/Transactions/{id}` - Update an existing transaction
* `DELETE /api/Transactions/{id}` - Delete a transaction
