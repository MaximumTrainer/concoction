# Concoction

![Build](https://img.shields.io/badge/build-passing-brightgreen) ![.NET 10](https://img.shields.io/badge/.NET-10-512BD4) ![License](https://img.shields.io/badge/license-MIT-blue)

Concoction is a .NET 10 synthetic data platform that discovers your database schema, generates realistic relational data with full referential integrity, and exports it in CSV, JSON, and SQL formats. It ships as a CLI, a REST API, and a TypeScript SDK, and supports a deterministic seeding model so the same seed always produces the same dataset.

### How it works

**Schema discovery.** Concoction connects to a live SQLite or PostgreSQL database and performs a read-only introspection using provider-specific adapters — `sqlite_master` and `pragma_*` views for SQLite; `information_schema` and `pg_catalog` for PostgreSQL. For every table it captures column names, SQL types, inferred data kinds (e.g. `Email`, `Integer`, `Guid`), nullability, primary keys, foreign keys, unique constraints, and indexes. The `discover` command prints this as JSON; `discover-profile` augments it with diagnostics such as self-referencing tables, cycle edges, and unmapped column types.

**Relational data generation.** Before producing any rows, Concoction analyses the foreign-key graph and builds a *generation plan*: tables are topologically sorted so parent tables are always populated before their dependents. Cycles in the FK graph are detected and broken at an optional FK column, with the cyclic reference backfilled after both sides exist. Self-referencing tables (e.g. `employees.manager_id → employees.id`) are handled with a chain strategy — row 0 gets a null root and each subsequent row references the previous one. All values are produced deterministically from a single integer seed, so the same seed and schema always produce the same dataset. Generation strategies are controlled by inferred data kinds, an optional YAML/JSON Rules DSL (per-column strategy, fixed value, null rate, weighted distribution, JSON-path rules), and a compliance profile (`Default`, `Healthcare`, or `Finance`) that applies masking for sensitive field categories. After generation, every FK reference, uniqueness constraint, and non-nullable column is validated, with any violations recorded in a structured issue list.

**Export.** The `generate` command writes all three formats in parallel to a single output directory: RFC-4180 **CSV** (one file per table, nulls as empty fields), **JSON** (an array of row objects per file), and **SQL** (standard `INSERT` statements with proper quoting, `NULL` literals, and `TRUE`/`FALSE` booleans). A `summary.json` artifact is written alongside them with table/row counts, timing, and a validation issue count. The `export` command targets a single format when only one is needed.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQLite or PostgreSQL (for schema discovery)
- Node.js 20+ (optional, for the TypeScript SDK only)

## Clone & Build

```bash
git clone https://github.com/MaximumTrainer/concoction.git
cd concoction
dotnet build Concoction.slnx
```

## Run Tests

```bash
dotnet test Concoction.slnx
```

81 tests (xUnit + FluentAssertions), all passing.

## CLI Quick Start

All CLI commands are invoked via `dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj --`.

### discover

Prints the discovered schema as JSON.

```bash
dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj -- discover \
  --provider sqlite \
  --connection "Data Source=./sample.db" \
  --database mydb \
  --seed 42
```

### discover-profile

Profiles the schema and prints diagnostic JSON (self-refs, cycles, unmapped columns).

```bash
dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj -- discover-profile \
  --provider sqlite \
  --connection "Data Source=./sample.db"
```

### generate

Discovers schema, generates synthetic rows, and writes CSV + JSON + SQL + `summary.json` to `--output`.

```bash
dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj -- generate \
  --provider sqlite \
  --connection "Data Source=./sample.db" \
  --seed 42 \
  --rows 100 \
  --rules ./rules.yaml \
  --compliance-profile Default \
  --output ./artifacts
```

### validate

Generates data and prints a validation summary; exits with code 3 if issues are found.

```bash
dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj -- validate \
  --provider sqlite \
  --connection "Data Source=./sample.db" \
  --rows 50
```

### export

Generates data and exports a single format to `--output`.

```bash
dotnet run --project ./Concoction.Cli/Concoction.Cli.csproj -- export \
  --provider sqlite \
  --connection "Data Source=./sample.db" \
  --format csv \
  --output ./csv-only
```

Supported `--format` values: `json`, `csv`, `sql`.

## Running the REST API

```bash
dotnet run --project ./Concoction.Api/Concoction.Api.csproj
```

The API starts on `http://localhost:5000` by default. Swagger UI is available at `http://localhost:5000/swagger`.

All endpoints require the header `X-Api-Key: cnc_<secret>`. See [docs/how-to/rest-api.md](docs/how-to/rest-api.md) for authentication setup.

## Solution Structure

| Project | Purpose |
|---|---|
| `Concoction.Domain` | Entities, value objects, enums, domain models |
| `Concoction.Application` | Use cases, port interfaces, orchestration |
| `Concoction.Infrastructure` | Adapters: SQLite/PostgreSQL providers, CSV/JSON/SQL exporters, in-memory repos |
| `Concoction.Cli` | `System.CommandLine` CLI (5 commands) |
| `Concoction.Api` | ASP.NET Core Minimal API (REST, Swagger) |
| `Concoction.Tests` | 81 xUnit + FluentAssertions tests |
| `sdk/typescript/` | `@concoction/client` npm package (CJS + ESM + DTS) |

## Extending Concoction

### Register a Custom Generator

Implement `IDataGenerator` in `Concoction.Application`, register it in the DI container via `ServiceCollectionExtensions`, and map your new `DataKind` values to it.

### Custom Rules

Write a `rules.yaml` file targeting specific tables and columns:

```yaml
version: "1"
tables:
  - table: "public.users"
    columns:
      - column: "email"
        strategy: "Email"
      - column: "status"
        fixedValue: "active"
```

Pass it with `--rules ./rules.yaml` on any command that supports it. See [docs/how-to/rules-dsl.md](docs/how-to/rules-dsl.md) for the full reference.

## Contributing

Pull requests are welcome. All contributions must:

1. Follow **Red → Green → Refactor** TDD.
2. Respect **hexagonal architecture** — no infrastructure imports in Domain or Application.
3. Pass `dotnet test Concoction.slnx` with no new failures.
4. Keep new public APIs covered by tests.

See [docs/user-guide.md](docs/user-guide.md) for full platform documentation.
