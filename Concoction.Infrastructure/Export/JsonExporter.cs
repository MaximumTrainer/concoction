using System.Text.Json;
using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Export;

public sealed class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public string Name => "json";

    public async Task ExportAsync(IReadOnlyList<TableData> tables, string target, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(target);
        foreach (var table in tables)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = Path.Combine(target, table.Table.Replace(".", "_", StringComparison.Ordinal) + ".json");
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, table.Rows, JsonOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
