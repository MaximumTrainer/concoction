# concoction

Concoction is a .NET 10/C# 12 synthetic data generation engine focused on schema fidelity, deterministic generation, and referential integrity.

## Projects

- `Concoction.Domain`: Core models and contracts.
- `Concoction.Application`: Use cases and synthesis logic.
- `Concoction.Infrastructure`: Database adapters, exporters, and DI wiring.
- `Concoction.Cli`: Command-line interface (`discover`, `generate`, `validate`, `export`).
- `Concoction.Tests`: Unit and integration tests.

## Quick start

```bash
dotnet build Concoction.slnx
dotnet test Concoction.slnx
```

### Example (SQLite)

```bash
dotnet run --project /home/runner/work/concoction/concoction/Concoction.Cli/Concoction.Cli.csproj -- discover --provider sqlite --connection "Data Source=/tmp/concoction.db"
```

## GitHub issue bootstrap

Run workflow `Bootstrap MVP Issues` with manifest path:

- `.github/issue-manifests/concoction-mvp.json`
