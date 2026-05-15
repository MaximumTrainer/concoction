using Concoction.Infrastructure.Configuration;
using Concoction.Infrastructure.Schema;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Concoction.Tests.Integration;

public sealed class SqliteSchemaProviderTests
{
    [Fact]
    public async Task DiscoverAsync_ShouldReturnTablesAndForeignKeys()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"concoction-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            var createSql = """
                            CREATE TABLE users (id INTEGER PRIMARY KEY, email TEXT NOT NULL UNIQUE);
                            CREATE TABLE orders (id INTEGER PRIMARY KEY, user_id INTEGER NOT NULL, FOREIGN KEY(user_id) REFERENCES users(id));
                            """;
            await using var command = new SqliteCommand(createSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        var options = Options.Create(new SchemaProviderOptions
        {
            Provider = "sqlite",
            ConnectionString = connectionString,
            DatabaseName = "fixture"
        });

        var provider = new SqliteSchemaProvider(options);
        var schema = await provider.DiscoverAsync();

        schema.Tables.Should().Contain(t => t.QualifiedName == "main.users");
        schema.Tables.Should().Contain(t => t.QualifiedName == "main.orders");
        schema.Tables.Single(t => t.QualifiedName == "main.orders").ForeignKeys.Should().NotBeEmpty();
    }
}
