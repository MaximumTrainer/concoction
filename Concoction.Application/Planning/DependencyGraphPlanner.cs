using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Application.Planning;

public sealed class DependencyGraphPlanner : IGenerationPlanner
{
    public GenerationPlan BuildPlan(DatabaseSchema schema)
    {
        var tableNames = schema.Tables.Select(static t => t.QualifiedName).OrderBy(static n => n, StringComparer.Ordinal).ToArray();
        var edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var table in tableNames)
        {
            edges[table] = [];
            inDegree[table] = 0;
        }

        foreach (var table in schema.Tables)
        {
            foreach (var fk in table.ForeignKeys)
            {
                if (edges.TryGetValue(fk.ReferencedTable, out var children) && children.Add(table.QualifiedName))
                {
                    inDegree[table.QualifiedName]++;
                }
            }
        }

        var queue = new Queue<string>(inDegree.Where(static x => x.Value == 0).Select(static x => x.Key).OrderBy(static x => x, StringComparer.Ordinal));
        var ordered = new List<string>(tableNames.Length);

        while (queue.TryDequeue(out var node))
        {
            ordered.Add(node);

            foreach (var child in edges[node].OrderBy(static x => x, StringComparer.Ordinal))
            {
                inDegree[child]--;
                if (inDegree[child] == 0)
                {
                    queue.Enqueue(child);
                }
            }
        }

        if (ordered.Count == tableNames.Length)
        {
            return new GenerationPlan(ordered, [], []);
        }

        var cycleTables = inDegree.Where(static x => x.Value > 0).Select(static x => x.Key).OrderBy(static x => x, StringComparer.Ordinal).ToArray();
        return new GenerationPlan(
            ordered.Concat(cycleTables).ToArray(),
            [cycleTables],
            [$"Cycle detected among tables: {string.Join(", ", cycleTables)}"]);
    }
}
