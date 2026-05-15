using System.ComponentModel.DataAnnotations;

namespace Concoction.Domain.Models;

public sealed record RuleConfiguration
{
    [Required]
    public required string Version { get; init; } = "1";

    [MinLength(0)]
    public IReadOnlyList<TableRule> Tables { get; init; } = [];
}

public sealed record TableRule
{
    [Required]
    public required string Table { get; init; }

    public IReadOnlyList<ColumnRule> Columns { get; init; } = [];
}

public sealed record ColumnRule
{
    [Required]
    public required string Column { get; init; }

    public string? Strategy { get; init; }

    public object? FixedValue { get; init; }

    [Range(0, 1)]
    public double? NullRate { get; init; }

    public int? SeedOffset { get; init; }

    public Dictionary<string, double>? Distribution { get; init; }
}
