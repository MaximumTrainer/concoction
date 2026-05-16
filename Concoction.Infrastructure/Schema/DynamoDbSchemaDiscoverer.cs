using Concoction.Application.Abstractions;
using Concoction.Domain.Models;

namespace Concoction.Infrastructure.Schema;

/// <summary>
/// Stub adapter for AWS DynamoDB schema discovery.
/// Full implementation is tracked in GitHub issue #55.
/// </summary>
/// <remarks>
/// Required NuGet package: AWSSDK.DynamoDBv2
/// Required configuration: AWS credentials via IAM role, environment variables, or AWS credentials file.
/// Recommended: use IAM roles for EC2/ECS/Lambda (never hardcode access keys).
/// </remarks>
public sealed class DynamoDbSchemaDiscoverer : INoSqlSchemaDiscoverer
{
    public string ProviderName => "dynamodb";

    public Task<IReadOnlyList<CollectionMetadata>> DiscoverCollectionsAsync(
        string connectionString,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            $"Provider '{ProviderName}': AWS DynamoDB schema discovery is not yet implemented. " +
            "Track progress at https://github.com/MaximumTrainer/synthetic-concoction/issues/55. " +
            "Requires NuGet package AWSSDK.DynamoDBv2 and AWS credentials (IAM role recommended).");
    }
}
