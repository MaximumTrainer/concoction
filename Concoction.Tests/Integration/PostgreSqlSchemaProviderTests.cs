using Concoction.Infrastructure.Configuration;
using Concoction.Infrastructure.Schema;
using FluentAssertions;
using Npgsql;
using Microsoft.Extensions.Options;

namespace Concoction.Tests.Integration;

public sealed class PostgreSqlSchemaProviderTests
{
    [Fact]
    public async Task DiscoverAsync_ShouldReturnCanonicalSchema_WhenConnectionProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONCOCTION_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var sql = """
                      DROP TABLE IF EXISTS public.orders;
                      DROP TABLE IF EXISTS public.users;
                      CREATE TABLE public.users (id SERIAL PRIMARY KEY, email TEXT UNIQUE NOT NULL);
                      CREATE TABLE public.orders (id SERIAL PRIMARY KEY, user_id INTEGER NOT NULL REFERENCES public.users(id));
                      """;
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }

        var provider = new PostgreSqlSchemaProvider(Options.Create(new SchemaProviderOptions
        {
            Provider = "postgres",
            ConnectionString = connectionString,
            DatabaseName = "fixture"
        }));

        var schema = await provider.DiscoverAsync();
        schema.Tables.Should().Contain(t => t.QualifiedName == "public.users");
        schema.Tables.Should().Contain(t => t.QualifiedName == "public.orders");
    }
}
