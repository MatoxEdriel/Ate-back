# 🗄️ Ate — Database Scripts (PostgreSQL 16 LTS)

This directory will contain the SQL scripts for the Ate database.

> **Convention:** All table names, column names, and constraints MUST be in **English**.

---

## 📁 Files

* **`01_init_schema.sql`**: Full DDL schema script creating tables (`roles`, `users`, `persons`, `user_roles`, `companies`, `employees`, `refresh_tokens`, `user_external_logins`), foreign keys, constraints, and filtered unique indexes.
* **`02_seed_data.sql`**: Idempotent SQL seed script creating:
  * Catalog roles (`SuperAdmin`, `CompanyAdmin`, `Employee`, `Merchant`).
  * Default SuperAdmin user:
    * **Email:** `admin@ate.com`
    * **Document:** `0943676155`
    * **Password:** `Admin123*Ate` (PBKDF2 SHA-256)
  * SuperAdmin person profile and role assignment.

---

## 🚀 How to run

### Option A: Docker Compose (Recommended for local dev)

```bash
docker compose up -d
```

### Option B: Manual execution with `psql`

```bash
psql -h localhost -U postgres -d ate_db -f backend/database/<script_name>.sql
```
