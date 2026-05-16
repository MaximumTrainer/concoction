using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Schema;

/// <summary>
/// Stub adapter for GCP Firestore schema discovery.
/// Full implementation is tracked in GitHub issue #56.
/// </summary>
/// <remarks>
/// Required NuGet package: Google.Cloud.Firestore
/// Required configuration: Application Default Credentials (ADC) via GOOGLE_APPLICATION_CREDENTIALS
/// or Workload Identity on GKE.
/// </remarks>
public sealed class FirestoreSchemaDiscoverer : INoSqlSchemaDiscoverer
{
    public string ProviderName => "firestore";

    public Task<IReadOnlyList<CollectionMetadata>> DiscoverCollectionsAsync(
        string connectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            $"Provider '{ProviderName}': GCP Firestore schema discovery is not yet implemented. " +
            "Track progress at https://github.com/MaximumTrainer/synthetic-concoction/issues/56. " +
            "Requires NuGet package Google.Cloud.Firestore and GCP Application Default Credentials.");
    }
}
