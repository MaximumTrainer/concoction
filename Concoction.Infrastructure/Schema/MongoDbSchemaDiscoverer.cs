using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Schema;

/// <summary>
/// Stub adapter for MongoDB schema discovery.
/// Full implementation is tracked in GitHub issue #54.
/// </summary>
/// <remarks>
/// Required NuGet package: MongoDB.Driver
/// Required configuration: MongoDB connection string (e.g. mongodb://host:27017 or Atlas SRV URI).
/// </remarks>
public sealed class MongoDbSchemaDiscoverer : INoSqlSchemaDiscoverer
{
    public string ProviderName => "mongodb";

    public Task<IReadOnlyList<CollectionMetadata>> DiscoverCollectionsAsync(
        string connectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            $"Provider '{ProviderName}': MongoDB schema discovery is not yet implemented. " +
            "Track progress at https://github.com/MaximumTrainer/synthetic-concoction/issues/54. " +
            "Requires NuGet package MongoDB.Driver and a valid MongoDB connection string.");
    }
}
