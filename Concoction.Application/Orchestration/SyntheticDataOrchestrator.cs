using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Application.Orchestration;

public sealed class SyntheticDataOrchestrator(
    ISchemaDiscoveryService schemaDiscoveryService,
    IGenerationPlanner planner,
    IRowMaterializer materializer,
    IConstraintEvaluator constraintEvaluator,
    ISensitiveFieldPolicy sensitiveFieldPolicy) : ISyntheticDataOrchestrator
{
    public Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default)
        => schemaDiscoveryService.DiscoverAsync(cancellationToken);

    public async Task<(GenerationResult Result, RunSummary Summary)> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var plan = planner.BuildPlan(request.Schema);

        var keyPool = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>(StringComparer.Ordinal);
        var tableData = new List<TableData>();
        var issues = new List<ValidationIssue>();
        var compliance = new List<ComplianceDecision>();

        foreach (var tableName in plan.OrderedTables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var table = request.Schema.Tables.First(t => string.Equals(t.QualifiedName, tableName, StringComparison.Ordinal));

            foreach (var column in table.Columns)
            {
                compliance.Add(sensitiveFieldPolicy.Evaluate(table.QualifiedName, column));
            }

            var rowCount = request.RequestedRowCounts.TryGetValue(table.QualifiedName, out var requested)
                ? requested
                : 10;

            var materialized = await materializer.MaterializeAsync(table, rowCount, request.Rules, keyPool, cancellationToken).ConfigureAwait(false);
            tableData.Add(materialized);

            var tableIssues = constraintEvaluator.Evaluate(table, materialized.Rows);
            issues.AddRange(tableIssues);

            if (table.PrimaryKey.Count > 0)
            {
                keyPool[table.QualifiedName] = materialized.Rows
                    .Select(row => (IReadOnlyDictionary<string, object?>)table.PrimaryKey
                        .ToDictionary(
                            col => col,
                            col => row.TryGetValue(col, out var val) ? val : null,
                            StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }
        }

        var result = new GenerationResult(tableData, issues, compliance);
        var summary = new RunSummary(
            startedAt,
            DateTimeOffset.UtcNow,
            tableData.Count,
            tableData.Sum(static t => t.Rows.Count),
            issues.Count,
            plan.Diagnostics);

        return (result, summary);
    }
}
