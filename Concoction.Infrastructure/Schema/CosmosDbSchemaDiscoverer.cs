using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Schema;

/// <summary>
/// Stub adapter for Azure Cosmos DB schema discovery.
/// Full implementation is tracked in GitHub issue #53.
/// </summary>
/// <remarks>
/// Required NuGet package: Microsoft.Azure.Cosmos
/// Required configuration: COSMOSDB_ENDPOINT + COSMOSDB_KEY or Managed Identity.
/// </remarks>
public sealed class CosmosDbSchemaDiscoverer : INoSqlSchemaDiscoverer
{
    public string ProviderName => "cosmosdb";

    public Task<IReadOnlyList<CollectionMetadata>> DiscoverCollectionsAsync(
        string connectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            $"Provider '{ProviderName}': Azure Cosmos DB schema discovery is not yet implemented. " +
            "Track progress at https://github.com/MaximumTrainer/synthetic-concoction/issues/53. " +
            "Requires NuGet package Microsoft.Azure.Cosmos and a valid Cosmos DB endpoint/key or Managed Identity.");
    }
}
