using System.Text;
using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Export;

public sealed class CsvExporter : IExporter
{
    public string Name => "csv";

    public async Task ExportAsync(IReadOnlyList<TableData> tables, string target, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(target);

        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = Path.Combine(target, table.Table.Replace(".", "_", StringComparison.Ordinal) + ".csv");

            if (table.Rows.Count == 0)
            {
                await File.WriteAllTextAsync(filePath, string.Empty, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var headers = table.Rows[0].Keys.OrderBy(static h => h, StringComparer.Ordinal).ToArray();
            var lines = new List<string> { string.Join(',', headers.Select(Escape)) };

            foreach (var row in table.Rows)
            {
                var values = headers.Select(h => Escape(row.TryGetValue(h, out var value) ? value : null));
                lines.Add(string.Join(',', values));
            }

            await File.WriteAllLinesAsync(filePath, lines, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains(',', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal) ||
            text.Contains('\r', StringComparison.Ordinal) || text.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return text;
    }
}
