-- schema.sql

CREATE TABLE users (
    id SERIAL PRIMARY KEY, 
    username VARCHAR(50) NOT NULL UNIQUE, 
    email VARCHAR(255) NOT NULL UNIQUE, 
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE accounts (
    id SERIAL PRIMARY KEY, 
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE, 
    account_name VARCHAR(100) NOT NULL, 
    account_type VARCHAR(30) NOT NULL,
    balance NUMERIC(12, 2) NOT NULL DEFAULT 0.00,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE transactions (
    id SERIAL PRIMARY KEY,
    account_id INT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    amount NUMERIC(12, 2) NOT NULL,
    type VARCHAR(10) NOT NULL,
    category VARCHAR(50),
    description TEXT,
    transaction_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE budgets (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    category VARCHAR(50) NOT NULL,
    monthly_limit NUMERIC(12, 2) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_user_category UNIQUE (user_id, category)
);
