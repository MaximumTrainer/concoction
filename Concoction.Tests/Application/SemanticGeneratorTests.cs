using Concoction.Application.Abstractions;
using Concoction.Application.Generation;
using Concoction.Domain.Enums;
using Concoction.Domain.Models;
using FluentAssertions;

namespace Concoction.Tests.Application;

public sealed class SemanticGeneratorTests
{
    private static GeneratorContext MakeContext(DataKind kind, int row = 0) =>
        new("table", "col", kind, row, null,
            new Dictionary<string, object?>(),
            new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>>());

    [Theory]
    [InlineData(DataKind.Email)]
    [InlineData(DataKind.Phone)]
    [InlineData(DataKind.Name)]
    [InlineData(DataKind.FirstName)]
    [InlineData(DataKind.LastName)]
    [InlineData(DataKind.Address)]
    [InlineData(DataKind.PostalCode)]
    [InlineData(DataKind.CountryCode)]
    [InlineData(DataKind.Url)]
    [InlineData(DataKind.IpAddress)]
    [InlineData(DataKind.Currency)]
    [InlineData(DataKind.CompanyName)]
    [InlineData(DataKind.Text)]
    [InlineData(DataKind.Uuid)]
    [InlineData(DataKind.TimestampTz)]
    public async Task GenerateAsync_ShouldProduceNonNullValueForSemanticKind(DataKind kind)
    {
        var registry = new GeneratorRegistry();
        registry.RegisterDefaults(new DeterministicRandomService(42));

        var context = MakeContext(kind);
        var resolved = registry.TryResolve(kind, null, out var generator);

        resolved.Should().BeTrue($"generator should be registered for {kind}");

        var value = await generator!(context, CancellationToken.None);
        value.Should().NotBeNull($"generator for {kind} should produce a value");
        value!.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EmailGenerator_ShouldProduceValidEmailPattern()
    {
        var registry = new GeneratorRegistry();
        registry.RegisterDefaults(new DeterministicRandomService(42));
        registry.TryResolve(DataKind.Email, null, out var gen);

        var value = (string)(await gen!(MakeContext(DataKind.Email), CancellationToken.None))!;
        value.Should().Contain("@");
        value.Should().Contain(".");
    }

    [Fact]
    public async Task UuidGenerator_ShouldProduceValidGuid()
    {
        var registry = new GeneratorRegistry();
        registry.RegisterDefaults(new DeterministicRandomService(42));
        registry.TryResolve(DataKind.Uuid, null, out var gen);

        var value = (string)(await gen!(MakeContext(DataKind.Uuid), CancellationToken.None))!;
        Guid.TryParse(value, out _).Should().BeTrue();
    }

    [Fact]
    public async Task SemanticGenerators_ShouldBeDeterministic()
    {
        var registry1 = new GeneratorRegistry();
        registry1.RegisterDefaults(new DeterministicRandomService(99));
        var registry2 = new GeneratorRegistry();
        registry2.RegisterDefaults(new DeterministicRandomService(99));

        foreach (var kind in new[] { DataKind.Email, DataKind.Name, DataKind.CountryCode })
        {
            registry1.TryResolve(kind, null, out var gen1);
            registry2.TryResolve(kind, null, out var gen2);

            var ctx = MakeContext(kind, 5);
            var v1 = await gen1!(ctx, CancellationToken.None);
            var v2 = await gen2!(ctx, CancellationToken.None);

            v1.Should().Be(v2, $"same seed should produce same value for {kind}");
        }
    }
}
