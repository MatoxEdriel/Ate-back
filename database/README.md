# 🗄️ Ate — Database Scripts (PostgreSQL 16 LTS)

This directory will contain the SQL scripts for the Ate database.

> **Convention:** All table names, column names, and constraints MUST be in **English**.

---

## 📁 Files

Scripts will be added here as the database schema is defined.

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
