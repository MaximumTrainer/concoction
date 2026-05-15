using Concoction.Application.Abstractions;
using Concoction.Domain.Enums;

namespace Concoction.Application.Generation;

public static class DefaultGeneratorRegistration
{
    public static IGeneratorRegistry RegisterDefaults(this IGeneratorRegistry registry, IRandomService random)
    {
        registry.Register(DataKind.Boolean, (ctx, _) => new ValueTask<object?>(random.NextInt(Scope(ctx), 0, 2) == 1));
        registry.Register(DataKind.Integer, (ctx, _) => new ValueTask<object?>(random.NextInt(Scope(ctx), 1, 100_000)));
        registry.Register(DataKind.Long, (ctx, _) => new ValueTask<object?>(random.NextLong(Scope(ctx), 1, long.MaxValue)));
        registry.Register(DataKind.Decimal, (ctx, _) => new ValueTask<object?>((decimal)(random.NextDouble(Scope(ctx)) * 10_000)));
        registry.Register(DataKind.Double, (ctx, _) => new ValueTask<object?>(random.NextDouble(Scope(ctx)) * 10_000));
        registry.Register(DataKind.String, (ctx, _) => new ValueTask<object?>($"{ctx.Column}_{random.NextToken(Scope(ctx), 10)}"));
        registry.Register(DataKind.Guid, (ctx, _) => new ValueTask<object?>(random.NextGuid(Scope(ctx))));
        registry.Register(DataKind.Date, (ctx, _) => new ValueTask<object?>(DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(random.NextInt(Scope(ctx), -3650, 3650))));
        registry.Register(DataKind.DateTime, (ctx, _) => new ValueTask<object?>(DateTimeOffset.UtcNow.AddMinutes(random.NextInt(Scope(ctx), -1_000_000, 1_000_000))));
        registry.Register(DataKind.Json, (ctx, _) => new ValueTask<object?>($"{{\"id\":\"{random.NextToken(Scope(ctx), 8)}\"}}"));
        registry.Register(DataKind.Binary, (ctx, _) =>
        {
            var payload = random.NextToken(Scope(ctx), 16);
            return new ValueTask<object?>(System.Text.Encoding.UTF8.GetBytes(payload));
        });
        return registry;
    }

    private static string Scope(GeneratorContext context)
        => $"{context.Table}.{context.Column}.{context.RowIndex}";
}
