using Concoction.Application.Abstractions;
using Concoction.Domain.Models;
using Concoction.Infrastructure.Schema;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Concoction.Tests.Application;

public sealed class NoSqlProviderTests
{
    // ── Domain model tests ─────────────────────────────────────────────────────

    [Fact]
    public void CollectionMetadata_QualifiedName_CombinesDatabaseAndCollectionName()
    {
        var metadata = new CollectionMetadata(
            DatabaseName: "mydb",
            CollectionName: "users",
            Fields: [],
            PartitionKey: null,
            Indexes: []);

        metadata.QualifiedName.Should().Be("mydb.users");
    }

    [Fact]
    public void FieldDescriptor_WithNestedFields_ReturnsCorrectHierarchy()
    {
        var nested = new FieldDescriptor("city", DocumentFieldType.String, IsNullable: false);
        var address = new FieldDescriptor("address", DocumentFieldType.Object, IsNullable: true, NestedFields: [nested]);

        address.NestedFields.Should().HaveCount(1);
        address.NestedFields![0].Name.Should().Be("city");
        address.NestedFields![0].FieldType.Should().Be(DocumentFieldType.String);
    }

    [Fact]
    public void PartitionKeyDescriptor_StoresFieldPathAndKeyType()
    {
        var pk = new PartitionKeyDescriptor("/userId", "Hash");

        pk.FieldPath.Should().Be("/userId");
        pk.KeyType.Should().Be("Hash");
    }

    [Fact]
    public void CollectionIndexDescriptor_StoresIndexDetails()
    {
        var index = new CollectionIndexDescriptor("idx_email", ["email"], IsUnique: true, IsSparse: false);

        index.Name.Should().Be("idx_email");
        index.FieldPaths.Should().ContainSingle("email");
        index.IsUnique.Should().BeTrue();
    }

    // ── Stub adapter tests ────────────────────────────────────────────────────

    [Theory]
    [InlineData("cosmosdb")]
    [InlineData("mongodb")]
    [InlineData("dynamodb")]
    [InlineData("firestore")]
    public async Task Stub_DiscoverCollectionsAsync_ThrowsNotSupportedException(string providerName)
    {
        INoSqlSchemaDiscoverer stub = providerName switch
        {
            "cosmosdb" => new CosmosDbSchemaDiscoverer(),
            "mongodb" => new MongoDbSchemaDiscoverer(),
            "dynamodb" => new DynamoDbSchemaDiscoverer(),
            "firestore" => new FirestoreSchemaDiscoverer(),
            _ => throw new ArgumentException($"Unknown provider: {providerName}")
        };

        var act = async () => await stub.DiscoverCollectionsAsync("conn", "db");

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*{stub.ProviderName}*");
    }

    [Theory]
    [InlineData("cosmosdb")]
    [InlineData("mongodb")]
    [InlineData("dynamodb")]
    [InlineData("firestore")]
    public void Stub_ProviderName_MatchesExpected(string providerName)
    {
        INoSqlSchemaDiscoverer stub = providerName switch
        {
            "cosmosdb" => new CosmosDbSchemaDiscoverer(),
            "mongodb" => new MongoDbSchemaDiscoverer(),
            "dynamodb" => new DynamoDbSchemaDiscoverer(),
            "firestore" => new FirestoreSchemaDiscoverer(),
            _ => throw new ArgumentException($"Unknown provider: {providerName}")
        };

        stub.ProviderName.Should().Be(providerName);
    }

    // ── Factory tests ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("cosmosdb")]
    [InlineData("CosmosDB")]   // case-insensitive
    [InlineData("mongodb")]
    [InlineData("MongoDB")]
    [InlineData("dynamodb")]
    [InlineData("firestore")]
    public void Factory_GetDiscoverer_ResolvesRegisteredProviders(string providerName)
    {
        var factory = BuildFactory();

        var discoverer = factory.GetDiscoverer(providerName);

        discoverer.Should().NotBeNull();
    }

    [Fact]
    public void Factory_GetDiscoverer_ThrowsForUnknownProvider()
    {
        var factory = BuildFactory();

        var act = () => factory.GetDiscoverer("oracle");

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*oracle*");
    }

    [Fact]
    public void Factory_GetDiscoverer_MessageIncludesKnownProviders()
    {
        var factory = BuildFactory();

        var act = () => factory.GetDiscoverer("cassandra");

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*cosmosdb*");
    }

    private static INoSqlSchemaDiscovererFactory BuildFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INoSqlSchemaDiscoverer, CosmosDbSchemaDiscoverer>();
        services.AddSingleton<INoSqlSchemaDiscoverer, MongoDbSchemaDiscoverer>();
        services.AddSingleton<INoSqlSchemaDiscoverer, DynamoDbSchemaDiscoverer>();
        services.AddSingleton<INoSqlSchemaDiscoverer, FirestoreSchemaDiscoverer>();
        services.AddSingleton<INoSqlSchemaDiscovererFactory, NoSqlSchemaDiscovererFactory>();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<INoSqlSchemaDiscovererFactory>();
    }
}
