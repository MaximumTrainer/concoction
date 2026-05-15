using System.CommandLine;
using System.Text.Json;
using Concoction.Application.Abstractions;
using Concoction.Domain.Enums;
using Concoction.Domain.Models;
using Concoction.Infrastructure.Configuration;
using Concoction.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var root = new RootCommand("Concoction synthetic data CLI");

var providerOption = new Option<string>("--provider", getDefaultValue: () => "sqlite", description: "Schema provider: sqlite or postgres");
var connectionOption = new Option<string>("--connection", "Database connection string") { IsRequired = true };
var dbNameOption = new Option<string>("--database", getDefaultValue: () => "concoction", description: "Database name in synthetic model");
var seedOption = new Option<long>("--seed", getDefaultValue: () => 42L, description: "Deterministic seed");
var rulesOption = new Option<string?>("--rules", "Path to rule configuration JSON/YAML");
var outputOption = new Option<string>("--output", getDefaultValue: () => "./artifacts", description: "Output directory");
var rowsOption = new Option<int>("--rows", getDefaultValue: () => 10, description: "Default row count per table");
var complianceOption = new Option<ComplianceProfile>("--compliance-profile", getDefaultValue: () => ComplianceProfile.Default, description: "Compliance profile: Default, Healthcare (HIPAA), Finance (PCI)");

var discover = new Command("discover", "Discover schema")
{
    providerOption, connectionOption, dbNameOption, seedOption
};

discover.SetHandler(async (provider, connection, database, seed) =>
{
    using var host = BuildHost(provider, connection, database, seed);
    var orchestrator = host.Services.GetRequiredService<ISyntheticDataOrchestrator>();
    var schema = await orchestrator.DiscoverAsync();
    Console.WriteLine(JsonSerializer.Serialize(schema, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
}, providerOption, connectionOption, dbNameOption, seedOption);

var generate = new Command("generate", "Discover + generate + export")
{
    providerOption, connectionOption, dbNameOption, seedOption, rulesOption, outputOption, rowsOption, complianceOption
};

generate.SetHandler(async (provider, connection, database, seed, rules, output, rows, compliance) =>
{
    using var host = BuildHost(provider, connection, database, seed);
    var services = host.Services;
    var orchestrator = services.GetRequiredService<ISyntheticDataOrchestrator>();
    var ruleService = services.GetRequiredService<IRuleConfigurationService>();
    var exporters = services.GetServices<IExporter>().ToArray();

    var schema = await orchestrator.DiscoverAsync();
    var rowCounts = schema.Tables.ToDictionary(static t => t.QualifiedName, _ => rows, StringComparer.Ordinal);

    RuleConfiguration? loadedRules = null;
    if (!string.IsNullOrWhiteSpace(rules))
    {
        loadedRules = ruleService.Load(rules!);
        var validationErrors = ruleService.Validate(loadedRules);
        if (validationErrors.Count > 0)
        {
            foreach (var err in validationErrors)
            {
                Console.Error.WriteLine(err);
            }

            Environment.ExitCode = 1;
            return;
        }

        // Apply precedence merge: defaults < schema-derived < user rules.
        var emptyConfig = new RuleConfiguration { Version = loadedRules.Version, Tables = [] };
        loadedRules = ruleService.Merge(emptyConfig, emptyConfig, loadedRules);
    }

    var request = new GenerationRequest(schema, rowCounts, seed, loadedRules, compliance);
    var (result, summary) = await orchestrator.GenerateAsync(request);

    foreach (var exporter in exporters)
    {
        await exporter.ExportAsync(result.Tables, Path.Combine(output, exporter.Name));
    }

    var summaryPath = Path.Combine(output, "summary.json");
    Directory.CreateDirectory(output);
    await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

    Console.WriteLine($"Generated {summary.RowCount} rows across {summary.TableCount} tables.");

    if (!result.IsSuccess)
    {
        Console.Error.WriteLine($"Validation issues: {result.ValidationIssues.Count}");
        Environment.ExitCode = 2;
    }
}, providerOption, connectionOption, dbNameOption, seedOption, rulesOption, outputOption, rowsOption, complianceOption);

var validate = new Command("validate", "Generate and return validation summary")
{
    providerOption, connectionOption, dbNameOption, seedOption, rowsOption
};

validate.SetHandler(async (provider, connection, database, seed, rows) =>
{
    using var host = BuildHost(provider, connection, database, seed);
    var orchestrator = host.Services.GetRequiredService<ISyntheticDataOrchestrator>();
    var schema = await orchestrator.DiscoverAsync();
    var rowCounts = schema.Tables.ToDictionary(static t => t.QualifiedName, _ => rows, StringComparer.Ordinal);

    var (result, summary) = await orchestrator.GenerateAsync(new GenerationRequest(schema, rowCounts, seed));
    Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

    if (!result.IsSuccess)
    {
        foreach (var issue in result.ValidationIssues)
        {
            Console.Error.WriteLine($"{issue.Table}.{issue.Column}: {issue.Reason}");
        }

        Environment.ExitCode = 3;
    }
}, providerOption, connectionOption, dbNameOption, seedOption, rowsOption);

var export = new Command("export", "Generate and export using chosen format")
{
    providerOption, connectionOption, dbNameOption, seedOption, outputOption, rowsOption
};

var formatOption = new Option<string>("--format", getDefaultValue: () => "json", description: "Export format: json or csv");
export.AddOption(formatOption);

export.SetHandler(async (provider, connection, database, seed, output, rows, format) =>
{
    using var host = BuildHost(provider, connection, database, seed);
    var orchestrator = host.Services.GetRequiredService<ISyntheticDataOrchestrator>();
    var exporters = host.Services.GetServices<IExporter>().Where(e => string.Equals(e.Name, format, StringComparison.OrdinalIgnoreCase)).ToArray();

    if (exporters.Length == 0)
    {
        Console.Error.WriteLine($"Unsupported exporter '{format}'.");
        Environment.ExitCode = 1;
        return;
    }

    var schema = await orchestrator.DiscoverAsync();
    var rowCounts = schema.Tables.ToDictionary(static t => t.QualifiedName, _ => rows, StringComparer.Ordinal);
    var (result, _) = await orchestrator.GenerateAsync(new GenerationRequest(schema, rowCounts, seed));

    foreach (var exporter in exporters)
    {
        await exporter.ExportAsync(result.Tables, output);
    }

    if (!result.IsSuccess)
    {
        Environment.ExitCode = 2;
    }
}, providerOption, connectionOption, dbNameOption, seedOption, outputOption, rowsOption, formatOption);

var discoverProfile = new Command("discover-profile", "Profile schema data distribution and surface diagnostics")
{
    providerOption, connectionOption, dbNameOption, seedOption
};

discoverProfile.SetHandler(async (provider, connection, database, seed) =>
{
    using var host = BuildHost(provider, connection, database, seed);
    var orchestrator = host.Services.GetRequiredService<ISyntheticDataOrchestrator>();
    var reviewService = host.Services.GetRequiredService<ISchemaReviewService>();
    var schema = await orchestrator.DiscoverAsync();
    var diagnostics = reviewService.Review(schema);

    Console.WriteLine(JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

    if (diagnostics.Diagnostics.Count > 0)
    {
        Console.Error.WriteLine($"{diagnostics.Diagnostics.Count} diagnostic(s) found. See output for details.");
        Environment.ExitCode = 1;
    }
}, providerOption, connectionOption, dbNameOption, seedOption);

root.AddCommand(discover);
root.AddCommand(discoverProfile);
root.AddCommand(generate);
root.AddCommand(validate);
root.AddCommand(export);

return await root.InvokeAsync(args);

static IHost BuildHost(string provider, string connectionString, string database, long seed)
{
    return Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddConcoctionApplication(seed);
            services.AddConcoctionInfrastructure(options =>
            {
                options.Provider = provider;
                options.ConnectionString = connectionString;
                options.DatabaseName = database;
            });
        })
        .Build();
}
