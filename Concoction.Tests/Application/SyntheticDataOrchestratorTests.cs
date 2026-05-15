using Concoction.Application.Abstractions;
using Concoction.Application.Compliance;
using Concoction.Application.Constraints;
using Concoction.Application.Generation;
using Concoction.Application.Orchestration;
using Concoction.Application.Planning;
using Concoction.Application.Schema;
using Concoction.Domain.Enums;
using Concoction.Domain.Models;
using FluentAssertions;

namespace Concoction.Tests.Application;

public sealed class SyntheticDataOrchestratorTests
{
    [Fact]
    public async Task GenerateAsync_ShouldProduceReferentiallyConnectedRows()
    {
        var schema = new DatabaseSchema("fixture",
        [
            new TableSchema("main", "users",
            [
                new ColumnSchema("id", "int", DataKind.Integer, false, true, true, null, null, null, null),
                new ColumnSchema("email", "text", DataKind.String, false, false, true, 50, null, null, null)
            ],
            ["id"], [], [new UniqueConstraintSchema("uq_users_email", ["email"])], []),
            new TableSchema("main", "orders",
            [
                new ColumnSchema("id", "int", DataKind.Integer, false, true, true, null, null, null, null),
                new ColumnSchema("user_id", "int", DataKind.Integer, false, false, false, null, null, null, null)
            ],
            ["id"],
            [new ForeignKeySchema("fk_orders_users", "main.orders", ["user_id"], "main.users", ["id"])],
            [], [])
        ]);

        var provider = new StubSchemaProvider(schema);
        var schemaService = new SchemaDiscoveryService(provider);
        var random = new DeterministicRandomService(99);
        var registry = new GeneratorRegistry();
        registry.RegisterDefaults(random);

        var orchestrator = new SyntheticDataOrchestrator(
            schemaService,
            new DependencyGraphPlanner(),
            new ReferentialRowMaterializer(registry, random),
            new ConstraintEvaluator(),
            new DefaultSensitiveFieldPolicy());

        var request = new GenerationRequest(schema,
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["main.users"] = 5,
                ["main.orders"] = 10
            },
            99);

        var (result, summary) = await orchestrator.GenerateAsync(request);

        summary.TableCount.Should().Be(2);
        result.Tables.Should().HaveCount(2);
        result.Tables.Single(t => t.Table == "main.orders").Rows.Should().HaveCount(10);
    }

    private sealed class StubSchemaProvider(DatabaseSchema schema) : ISchemaProvider
    {
        public string ProviderName => "stub";
        public Task<DatabaseSchema> DiscoverAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(schema);
    }
}
