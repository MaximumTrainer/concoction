using Concoction.Domain.Enums;
using Concoction.Domain.Models;

namespace Concoction.Application.Abstractions;

public interface ISchemaProvider
{
    string ProviderName { get; }
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface ISchemaDiscoveryService
{
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}

public sealed record GeneratorContext(
    string Table,
    string Column,
    DataKind DataKind,
    int RowIndex,
    RuleConfiguration? Rules,
    IReadOnlyDictionary<string, object?> CurrentRow,
    IReadOnlyDictionary<string, IReadOnlyList<object>> ReferencePool);

public interface IValueGenerator<in TContext, TValue>
{
    ValueTask<TValue> GenerateAsync(TContext context, CancellationToken cancellationToken = default);
}

public interface IValueGeneratorDispatcher
{
    ValueTask<object?> GenerateAsync(GeneratorContext context, CancellationToken cancellationToken = default);
}

public interface IGeneratorRegistry
{
    void Register(DataKind kind, Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator, string? strategy = null);
    bool TryResolve(DataKind kind, string? strategy, out Func<GeneratorContext, CancellationToken, ValueTask<object?>> generator);
}

public interface IRandomService
{
    int NextInt(string scope, int minInclusive, int maxExclusive);
    long NextLong(string scope, long minInclusive, long maxExclusive);
    double NextDouble(string scope);
    string NextToken(string scope, int length);
    Guid NextGuid(string scope);
}

public interface IConstraintEvaluator
{
    IReadOnlyList<ValidationIssue> Evaluate(TableSchema table, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows);
}

public interface IGenerationPlanner
{
    GenerationPlan BuildPlan(DatabaseSchema schema);
}

public interface IRowMaterializer
{
    Task<TableData> MaterializeAsync(
        TableSchema table,
        int rowCount,
        RuleConfiguration? rules,
        IReadOnlyDictionary<string, IReadOnlyList<object>> keyPool,
        CancellationToken cancellationToken = default);
}

public interface IExporter
{
    string Name { get; }
    Task ExportAsync(IReadOnlyList<TableData> tables, string target, CancellationToken cancellationToken = default);
}

public interface ISensitiveFieldPolicy
{
    ComplianceDecision Evaluate(string table, ColumnSchema column);
}

public interface IRuleConfigurationService
{
    RuleConfiguration Load(string path);
    IReadOnlyList<string> Validate(RuleConfiguration configuration);
    RuleConfiguration Merge(RuleConfiguration defaults, RuleConfiguration schemaDerived, RuleConfiguration user);
}

public interface ISyntheticDataOrchestrator
{
    Task<(GenerationResult Result, RunSummary Summary)> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default);
    Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default);
}
