using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Application.Generation;

public sealed class ReferentialRowMaterializer(IValueGeneratorDispatcher dispatcher, IRandomService random) : IRowMaterializer
{
    public async Task<TableData> MaterializeAsync(
        TableSchema table,
        int rowCount,
        RuleConfiguration? rules,
        IReadOnlyDictionary<string, IReadOnlyList<object>> keyPool,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(capacity: rowCount);
        var uniqueValueSets = table.UniqueConstraints
            .ToDictionary(
                static u => string.Join('|', u.Columns.OrderBy(static c => c, StringComparer.OrdinalIgnoreCase)),
                _ => new HashSet<string>(StringComparer.Ordinal));

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in table.Columns)
            {
                object? value = null;

                var fk = table.ForeignKeys.FirstOrDefault(f => f.SourceColumns.Contains(column.Name, StringComparer.OrdinalIgnoreCase));
                if (fk is not null && keyPool.TryGetValue(fk.ReferencedTable, out var parentKeys) && parentKeys.Count > 0)
                {
                    value = parentKeys[random.NextInt($"fk:{table.QualifiedName}.{column.Name}", 0, parentKeys.Count)];
                }
                else
                {
                    value = await GenerateWithConstraintsAsync(table, column, rowIndex, rules, row, keyPool, uniqueValueSets, cancellationToken).ConfigureAwait(false);
                }

                row[column.Name] = value;
            }

            rows.Add(row);
        }

        return new TableData(table.QualifiedName, rows);
    }

    private async Task<object?> GenerateWithConstraintsAsync(
        TableSchema table,
        ColumnSchema column,
        int rowIndex,
        RuleConfiguration? rules,
        Dictionary<string, object?> row,
        IReadOnlyDictionary<string, IReadOnlyList<object>> keyPool,
        Dictionary<string, HashSet<string>> uniqueValueSets,
        CancellationToken cancellationToken)
    {
        var columnRule = rules?.Tables
            .FirstOrDefault(t => string.Equals(t.Table, table.QualifiedName, StringComparison.OrdinalIgnoreCase))
            ?.Columns.FirstOrDefault(c => string.Equals(c.Column, column.Name, StringComparison.OrdinalIgnoreCase));

        if (columnRule?.FixedValue is not null)
        {
            return columnRule.FixedValue;
        }

        if (column.IsNullable && columnRule?.NullRate is double nullRate && random.NextDouble($"null:{table.QualifiedName}.{column.Name}.{rowIndex}") <= nullRate)
        {
            return null;
        }

        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var context = new GeneratorContext(table.QualifiedName, column.Name, column.DataKind, rowIndex + attempt, rules, row, keyPool);
            var candidate = await dispatcher.GenerateAsync(context, cancellationToken).ConfigureAwait(false);

            if (candidate is string text && column.MaxLength is int maxLength && text.Length > maxLength)
            {
                candidate = text[..maxLength];
            }

            if (column.AllowedValues is { Count: > 0 } allowedValues && candidate is not null)
            {
                candidate = allowedValues[random.NextInt($"enum:{table.QualifiedName}.{column.Name}.{rowIndex}", 0, allowedValues.Count)];
            }

            if (column.IsUnique)
            {
                var setKey = string.Join('|', table.UniqueConstraints
                    .Where(u => u.Columns.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
                    .SelectMany(static u => u.Columns)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static c => c, StringComparer.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(setKey))
                {
                    setKey = column.Name;
                }

                if (!uniqueValueSets.TryGetValue(setKey, out var used))
                {
                    used = new HashSet<string>(StringComparer.Ordinal);
                    uniqueValueSets[setKey] = used;
                }

                var key = candidate?.ToString() ?? "<null>";
                if (!used.Add(key))
                {
                    continue;
                }
            }

            return candidate;
        }

        throw new InvalidOperationException($"Unable to generate unique value for {table.QualifiedName}.{column.Name} after {maxAttempts} attempts.");
    }
}
