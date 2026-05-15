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
                compliance.Add(sensitiveFieldPolicy.Evaluate(table.QualifiedName, column, request.ComplianceProfile));
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

        // Backfill self-referencing FK columns now that all rows exist.
        // Row 0 in each table gets null (tree root); subsequent rows reference a random earlier row.
        if (plan.SelfReferencingTables.Count > 0)
        {
            BackfillSelfReferences(request.Schema, plan.SelfReferencingTables, tableData, issues);
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

    private static void BackfillSelfReferences(
        DatabaseSchema schema,
        IReadOnlyList<string> selfRefTableNames,
        List<TableData> tableData,
        List<ValidationIssue> issues)
    {
        foreach (var tableName in selfRefTableNames)
        {
            var tableSchema = schema.Tables.FirstOrDefault(t => string.Equals(t.QualifiedName, tableName, StringComparison.Ordinal));
            if (tableSchema is null) continue;

            var selfRefFks = tableSchema.ForeignKeys
                .Where(fk => string.Equals(fk.ReferencedTable, tableName, StringComparison.Ordinal))
                .ToArray();
            if (selfRefFks.Length == 0) continue;

            var idx = tableData.FindIndex(t => string.Equals(t.Table, tableName, StringComparison.Ordinal));
            if (idx < 0) continue;

            var existing = tableData[idx];
            var rows = existing.Rows;

            if (rows.Count == 0) continue;

            // Collect PK values for reference
            var pkColumn = tableSchema.PrimaryKey.Count > 0 ? tableSchema.PrimaryKey[0] : null;

            var updatedRows = rows.Select((row, rowIndex) =>
            {
                var mutable = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);

                foreach (var fk in selfRefFks)
                {
                    // Row 0 is the root — nullable FK gets null; non-nullable gets self-ref to row 0 (same row if only 1 row).
                    // Rows 1+ reference a random earlier row to form a valid forest.
                    var parentRowIndex = rowIndex == 0 ? -1 : (rowIndex % rowIndex); // will resolve below

                    // Simple parent selection: row N references row (N-1) % rowCount, creating a chain.
                    // Row 0 gets null (root of tree).
                    if (rowIndex > 0 && pkColumn is not null)
                    {
                        var parentRow = rows[rowIndex - 1];
                        for (var colIdx = 0; colIdx < fk.SourceColumns.Count && colIdx < fk.ReferencedColumns.Count; colIdx++)
                        {
                            var sourceCol = fk.SourceColumns[colIdx];
                            var refCol = fk.ReferencedColumns[colIdx];
                            mutable[sourceCol] = parentRow.TryGetValue(refCol, out var refVal) ? refVal : null;
                        }
                    }
                    else
                    {
                        // Root row — ensure FK columns are null (require nullable FK)
                        foreach (var sourceCol in fk.SourceColumns)
                        {
                            var colSchema = tableSchema.Columns.FirstOrDefault(c => string.Equals(c.Name, sourceCol, StringComparison.OrdinalIgnoreCase));
                            if (colSchema?.IsNullable == true)
                            {
                                mutable[sourceCol] = null;
                            }
                            else
                            {
                                issues.Add(new ValidationIssue(tableName, sourceCol,
                                    $"Self-referencing FK '{fk.Name}' on non-nullable column '{sourceCol}' cannot be backfilled for root row. Mark column nullable."));
                            }
                        }
                    }
                }

                return (IReadOnlyDictionary<string, object?>)mutable;
            }).ToList();

            tableData[idx] = new TableData(tableName, updatedRows);
        }
    }
}