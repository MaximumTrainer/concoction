using Concoction.Domain.Enums;

namespace Concoction.Domain.Models;

public sealed record GenerationRequest(
    DatabaseSchema Schema,
    IReadOnlyDictionary<string, int> RequestedRowCounts,
    long Seed,
    RuleConfiguration? Rules = null);

public sealed record TableData(string Table, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);

public sealed record GenerationResult(
    IReadOnlyList<TableData> Tables,
    IReadOnlyList<ValidationIssue> ValidationIssues,
    IReadOnlyList<ComplianceDecision> ComplianceDecisions)
{
    public bool IsSuccess => ValidationIssues.Count == 0;
}

public sealed record ValidationIssue(string Table, string Column, string Reason);

public sealed record GenerationPlan(
    IReadOnlyList<string> OrderedTables,
    IReadOnlyList<IReadOnlyList<string>> Cycles,
    IReadOnlyList<string> Diagnostics);

public sealed record RunSummary(
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int TableCount,
    int RowCount,
    int ValidationIssueCount,
    IReadOnlyList<string> Messages);

public sealed record ComplianceDecision(
    string Table,
    string Column,
    SensitiveFieldStrategy Strategy,
    string Classification,
    string Reason);
